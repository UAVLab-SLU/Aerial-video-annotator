

import UdpComms as U
import time
import cv2
import base64
import imutils
import json
import pandas as pd
import GeoCoordinationHandler as GC
import math
from pyquaternion import Quaternion
import requests
# Create UDP socket to use for sending (and receiving)
sock = U.UdpComms(udpIP="127.0.0.1", portTX=8080, portRX=8001, enableRX=True, suppressWarnings=True)
sock2 = U.UdpComms(udpIP="127.0.0.1", portTX=8000, portRX=8002, enableRX=True, suppressWarnings=True)
cam = cv2.VideoCapture('Samples/east_1.mp4')
i = 1
lat = 50
lon = 50
alt = 40

df = pd.read_csv('Samples/east_1.csv')

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

while True:
    ret,camImage = cam.read()

    frame = imutils.resize(camImage,width=400)
    encoded,buffer = cv2.imencode('.jpg',frame,[cv2.IMWRITE_JPEG_QUALITY,80])
    byteString = base64.b64encode(buffer)

    frameBytes = buffer.tobytes()
    encoded_string= base64.b64encode(frameBytes)
    # byteString = bytes(cv2.imencode('.jpg', buffer)[1].tostring())
    print('...',i)
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
                'alt' : str(intersect_LLA[2])
            }
            sock2.SendData(json.dumps(di2).encode('utf-8'))

            db_di ={
                 'lat' : float(str(intersect_LLA[0])),
                'lon' : float(str(intersect_LLA[1])),
                'obj':int(dat["obj"])
            }
            post_data(db_di)

    dat2 = sock2.ReadReceivedData() # read data
    #implement timer function
    if dat2 != None: 
            print(type(dat)) 
            print(dat)
            

    time.sleep(0.1)


