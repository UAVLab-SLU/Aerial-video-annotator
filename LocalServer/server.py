from __future__ import absolute_import
from __future__ import division
from __future__ import print_function
from __future__ import unicode_literals

import os
import sys
sys.path.append('../')

import argparse
import cv2
import torch
from glob import glob

from pysot.core.config_adapn import cfg
from pysot.models.model_builder_adapn import ModelBuilderADAPN
from pysot.tracker.adsiamapn_tracker import ADSiamAPNTracker
from pysot.utils.model_load import load_pretrain
from collections import deque

torch.set_num_threads(1)


import time
import UdpComms as U
import time
import cv2
import base64
import imutils
import json
import pandas as pd
import GeoCoordinationHandler as GC
from object_detector import ObjectDetector
import requests
# Create UDP socket to use for sending (and receiving)
sock = U.UdpComms(udpIP="127.0.0.1", portTX=8080, portRX=8081, enableRX=True, suppressWarnings=True)
sock2 = U.UdpComms(udpIP="127.0.0.1", portTX=8000, portRX=8002, enableRX=True, suppressWarnings=True)
sock3 = U.UdpComms(udpIP="127.0.0.1", portTX=8005, portRX=8003, enableRX=True, suppressWarnings=True)
cam = cv2.VideoCapture('Samples/tracking.mp4')
i = 1
lat = 50
lon = 50
alt = 40

df = pd.read_csv('Samples/tracking.csv')


cfg.merge_from_file('config.yaml')
cfg.CUDA = torch.cuda.is_available()
device = torch.device('cuda' if cfg.CUDA else 'cpu')

# create model
model = ModelBuilderADAPN()

# load model
model = load_pretrain(model, 'SiamAPNPlusModel.pth').eval().to(device)

# build tracker

trackers = []
tracker_active = {}
# tracker = ADSiamAPNTracker(model)

detector = ObjectDetector()

track_di = {}
track_avg = {}

import numpy as np
def quaternion_to_euler(w, x, y, z):
    ysqr = y * y

    t0 = +2.0 * (w * x + y * z)
    t1 = +1.0 - 2.0 * (x * x + ysqr)
    X = np.degrees(np.arctan2(t0, t1))

    t2 = +2.0 * (w * y - z * x)
    t2 = np.where(t2>+1.0,+1.0,t2)
    #t2 = +1.0 if t2 > +1.0 else t2

    t2 = np.where(t2<-1.0, -1.0, t2)
    #t2 = -1.0 if t2 < -1.0 else t2
    Y = np.degrees(np.arcsin(t2))

    t3 = +2.0 * (w * z + x * y)
    t4 = +1.0 - 2.0 * (ysqr + z * z)
    Z = np.degrees(np.arctan2(t3, t4))

    return X, Y, Z

database_url = "https://uavlab-98a0c-default-rtdb.firebaseio.com/"
def post_data(data):
    response = requests.post(database_url + "/location.json", json=data)
    print(response.status_code)


def bbox_intersection(boxA, boxB):
    # Determine the coordinates of the intersection rectangle
    xA = max(boxA[0], boxB[0])
    yA = max(boxA[1], boxB[1])
    xB = min(boxA[2], boxB[2])
    yB = min(boxA[3], boxB[3])

    # Compute the area of intersection
    intersection_area = max(0, xB - xA + 1) * max(0, yB - yA + 1)

    # Compute the area of each bounding box
    boxA_area = (boxA[2] - boxA[0] + 1) * (boxA[3] - boxA[1] + 1)
    boxB_area = (boxB[2] - boxB[0] + 1) * (boxB[3] - boxB[1] + 1)

    # Compute the IoU by taking the intersection area and dividing it
    # by the sum of the two areas minus the intersection area
    iou = intersection_area / float(boxA_area + boxB_area - intersection_area)
    # print(iou,'###########################################################')
    return iou

def bbox_to_string(trk,float_list):
    if trk == '':
        temp_li = []
        for li in float_list:
            int_list = [int(x) for x in li]
            st = '.'.join(map(str, int_list))
            # st = st+'u'
            temp_li.append(st)
        return 'n'.join(temp_li)
    else:
        traced_boxes = trk.split('n')

        temp_li = []
        for li in float_list:
            int_list = [int(x) for x in li]
            check = True
            for traced_b in traced_boxes:
                traced_split = traced_b.split('.')
                traced_box = [int(x) for x in traced_split]
                
                if bbox_intersection(li,traced_box)>0.3:
                    # print('overlapppppppppppp')
                    check = False
            if check:
                st = '.'.join(map(str, int_list))
                temp_li.append(st)
        return 'n'.join(temp_li)

# start_time = time.time()

# Tracker = False
    

def track(frame):
    if len(trackers) == 0:
        return ''
    else:
        temp_li = []
        for i in trackers:

            outputs = i.track(frame)
            

            # print(outputs['best_score'])
            track_di[i].append(outputs['best_score'])
            # print(track_di[i],'....................')
            bbox = list(map(int, outputs['bbox']))
            if bbox:
                bbox[2]=bbox[0]+bbox[2]
                bbox[3]=bbox[1]+bbox[3]
            st = '.'.join(map(str, bbox))
            temp_li.append(st)
            if len(track_di[i]) == 1:
                di = {}
                di["point"] = "start"
                di["box"] = st
                sock3.SendData(json.dumps(di).encode('utf-8'))
                print(st,"Starting pointtttttttttttttttttttttt")
            if len(track_di[i]) == 10:
                track_di[i].popleft()
                if sum(track_di[i])/10 <0.3:
                    # print("Siamesee low confidence")
                    trackers.remove(i)
                    tracker_active[i]=False
                    di = {}
                    di["point"] = "last"
                    di["box"] = st
                    sock3.SendData(json.dumps(di).encode('utf-8'))
                    print(st, "Last marked pointtttttttttttttttttttttt")
        return 'n'.join(temp_li)


def LocationCalculator(dat):
    c = GC.CameraRayProjection(69,[float(dat["lat"]),float(dat["lon"]),float(dat["alt"])],
                            [int(float(dat["resw"])),int(float(dat["resh"]))],
                            GC.Coordinates(int(float(dat["xpos"])),int(float(dat["ypos"]))),

                            [data['w'],data['x'],data['y'],data['z']])
    
    target_direction_ENU = c.target_ENU()
    target_direction_ECEF = c.ENU_to_ECEF(target_direction_ENU)
    intersect_ECEF = c.target_location(target_direction_ECEF)
    
    intersect_LLA = c.ECEFtoLLA(intersect_ECEF.x,intersect_ECEF.y,intersect_ECEF.z)
    print(c.LLAtoXYZ(intersect_LLA[0], intersect_LLA[1], intersect_LLA[2]))
    print("CALCULATED LOCATION IS",intersect_LLA)
    
    di2 = {
        'lat' : str(intersect_LLA[0]),
        'lon' : str(intersect_LLA[1]),
        'alt' : str(intersect_LLA[2]),
        'obj' : str(dat["obj"]),
        'ctr' : str(dat["ctr"])
    }
    return json.dumps(di2).encode('utf-8')

while True:
    ret,camImage = cam.read()
    height, width, _ = camImage.shape
    # print(camImage.shape)
    frame = imutils.resize(camImage,width=400)
    encoded,buffer = cv2.imencode('.jpg',frame,[cv2.IMWRITE_JPEG_QUALITY,80])
    byteString = base64.b64encode(buffer)

    frameBytes = buffer.tobytes()
    encoded_string= base64.b64encode(frameBytes)
    # byteString = bytes(cv2.imencode('.jpg', buffer)[1].tostring())
    # print('...',i)
    # if(i==501):
    #     end_time = time.time()

    #     # Calculate and print the elapsed time
    #     elapsed_time = end_time - start_time
    #     print(f"The program took {elapsed_time:.2f} seconds to run.")
    if(i>1):
        data = {
            'image':encoded_string.decode(),
            # 'image':'im',
        }
        di = df.iloc[i].to_dict()
        # print(di)
        data['lat'] = di['drone/location/latitude']
        data['lon'] =di['drone/location/longitude']
        data['alt'] = di['drone/ground_distance']

        data['w'] = di['camera/quat/w'] 
        data ['x'] =di['camera/quat/x']
        data ['y'] =di['camera/quat/y']
        data ['z'] =di['camera/quat/z']

        data['roll'],data['pitch'],data['yaw'] = quaternion_to_euler(data['w'],data['x'],data['y'],data['z'])

        #Object Detection

        res = detector.predict(camImage)
        # print(res[0].boxes)
        bbox_data = res[0].boxes.xyxy.cpu().numpy().tolist()
        # for box in bbox_data:
        # print(bbox_to_string(bbox_data))
        
        # data['bbox_data'] = "700.12.720.18"
        # print(bbox_to_string(bbox_data))
        # if Tracker == False:
        #     tracker.init(frame, (1061, 273, 34, 64))
        #     Tracker = True
        data['tracker'] = track(camImage)
        data['bbox_data'] = bbox_to_string(data['tracker'],bbox_data)
        # print(data['tracker'],'------------')
        data['tracker_status'] = '.'.join(['1' if value else '0' for value in tracker_active.values()])
        # print(data['tracker_status'],'+++++++++++++')
        # print('.'.join(['1' if value else '0' for value in tracker_active.values()]))
        # print(data['roll'],data['pitch'],data['yaw'])
        # print(di)
      

    
        sock.SendData(json.dumps(data).encode('utf-8')) # Send this string to other application
    i += 2


    dat = sock.ReadReceivedData() # read data

    if dat != None: # if NEW data has been received since last ReadReceivedData function call
            print(type(dat)) # print new received data
            print(dat)
            dat = "{"+dat+"}"
            dat = json.loads(dat)
            if dat["track"] == "":
                res = LocationCalculator(dat)
                # c = GC.CameraRayProjection(69,[float(dat["lat"]),float(dat["lon"]),float(dat["alt"])],
                #                         [int(float(dat["resw"])),int(float(dat["resh"]))],
                #                         GC.Coordinates(int(float(dat["xpos"])),int(float(dat["ypos"]))),

                #                         [data['w'],data['x'],data['y'],data['z']])
                
                # target_direction_ENU = c.target_ENU()
                # target_direction_ECEF = c.ENU_to_ECEF(target_direction_ENU)
                # intersect_ECEF = c.target_location(target_direction_ECEF)
                
                # intersect_LLA = c.ECEFtoLLA(intersect_ECEF.x,intersect_ECEF.y,intersect_ECEF.z)
                # print(c.LLAtoXYZ(intersect_LLA[0], intersect_LLA[1], intersect_LLA[2]))
                # print("CALCULATED LOCATION IS",intersect_LLA)
                
                # di2 = {
                #     'lat' : str(intersect_LLA[0]),
                #     'lon' : str(intersect_LLA[1]),
                #     'alt' : str(intersect_LLA[2]),
                #     'obj' : str(dat["obj"]),
                #     'ctr' : str(dat["ctr"])
                # }
                sock2.SendData(res)

            else:
                print("tracker initaliseddddddddddddddddddddd")
                li = list(map(int,dat["track"].split('.')))
                print(li)
                temp_tracker = ADSiamAPNTracker(model)
                trackers.append(temp_tracker)
                tracker_active[temp_tracker] = True
                track_di[temp_tracker] = deque(maxlen = 10)
                trackers[-1].init(frame, (li[0],li[1], li[2]-li[0], li[3]-li[1]))
                # tracker.init(frame, (li[0],li[1], li[2]-li[0], li[3]-li[1]))
                # Tracker = True

            # db_di ={
            #      'lat' : float(str(intersect_LLA[0])),
            #     'lon' : float(str(intersect_LLA[1])),
            #     'obj' : int(dat["obj"]),
            #     'ctr' : int(dat["ctr"])
            # }
            # post_data(db_di)

    dat2 = sock2.ReadReceivedData() # read data
    #implement timer function
    if dat2 != None: 
            print(type(dat2),"oooooooooooooooooooooooooo") 
            print(dat2)
            dat2 = "{"+dat2+"}"
            dat2 = json.loads(dat2)
            res = LocationCalculator(dat2)
            sock2.SendData(res)
            

    time.sleep(0.1)


