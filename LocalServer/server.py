

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
# Create UDP socket to use for sending (and receiving)
sock = U.UdpComms(udpIP="127.0.0.1", portTX=8080, portRX=8001, enableRX=True, suppressWarnings=True)
sock2 = U.UdpComms(udpIP="127.0.0.1", portTX=8000, portRX=8002, enableRX=True, suppressWarnings=True)
cam = cv2.VideoCapture('streaming_70.mp4')
i = 1
lat = 50
lon = 50
alt = 40

df = pd.read_csv('metadata_70.csv')


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

def ned_to_enu_quaternion(ned_quat):
    # NED to ENU conversion matrix
    conversion_matrix = np.array([[0, 1, 0, 0],
                                  [1, 0, 0, 0],
                                  [0, 0, -1, 0],
                                  [0, 0, 0, 1]])
    
    # Convert quaternion to a 4x1 matrix
    ned_quat_matrix = np.array([ned_quat[0], ned_quat[1], ned_quat[2], ned_quat[3]]).reshape(4, 1)
    
    # Perform the conversion: ENU = Conversion_Matrix * NED
    enu_quat_matrix = np.dot(conversion_matrix, ned_quat_matrix)
    
    # Convert the resulting 4x1 matrix back to a quaternion
    enu_quat = enu_quat_matrix.flatten()
    
    return enu_quat

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

        # data['drone_x'] = di['drone/local_position/x']
        # data['drone_y'] = di['drone/local_position/y']
        # data['drone_z'] = di['drone/local_position/z']

        
        # Unreal Quat Values - Camera
        # data['w'] = di['camera/quat/w'] * di['drone/quat/w'] 
        # data ['x'] =di['camera/quat/x'] * di['drone/quat/x'] 
        # data ['y'] =di['camera/quat/y'] * di['drone/quat/y'] 
        # data ['z'] =di['camera/quat/z'] * di['drone/quat/z'] 

         # Unreal Quat Values - Camera
        data['w'] = di['camera/quat/w'] 
        data ['x'] =di['camera/quat/x']
        data ['y'] =di['camera/quat/y']
        data ['z'] =di['camera/quat/z']

       
        unreal_camera_quat = Quaternion(data['w'],data['x'],data['y'],data['z'])
        # unreal_ned_quat_drone = Quaternion(float(di['drone/quat/w']),
        #                                        float(di['drone/quat/x']),
        #                                        float(di['drone/quat/y']),
        #                                        float(di['drone/quat/z']))
        
        # unreal_camera_final_quat = unreal_ned_quat_drone * unreal_camera_quat * unreal_ned_quat_drone.inverse
        unity_camera_quat = Quaternion()

        # Unreal to Unity Rotation Conversion
        # Step 1 : 90 degree counter clockwise along the z axis. 
        z_axis = [0,0,1]
        z_rotation = Quaternion(axis=z_axis, angle=np.pi/2)  # 90 degree rotation around z-axis
        unity_camera_quat = z_rotation * unreal_camera_quat

        # Step 2:  rotate it by 90 degree counter clockwise along the x axis
        x_axis = [1,0,0] 
        x_rotation = Quaternion(axis=x_axis, angle=np.pi/2)
        unity_camera_quat = x_rotation * unity_camera_quat

        # data['w'] = unity_camera_quat.x
        # data ['x'] =unity_camera_quat.y
        # data ['y'] =unity_camera_quat.z
        # data ['z'] =unity_camera_quat.w

        data['pitch'],data['yaw'],data['roll'] = quaternion_to_euler(unity_camera_quat.w, unity_camera_quat.w, unity_camera_quat.y, unity_camera_quat.z)
        
        # data['w'] = unity_camera_quat.w
        # data ['x'] =unity_camera_quat.x
        # data ['y'] =unity_camera_quat.y
        # data ['z'] =unity_camera_quat.z

    
        

        # Transform Unreal Quat in to Unreal Euler

        # data['roll'],data['pitch'],data['yaw'] = quaternion_to_euler(data['w'],data['x'],data['y'],data['z'])
        # print("Unreal Pitch and Yaw are".format(data['roll'],data['pitch'],data['yaw']))
        

        # Euler Roll pitch yaw
        data['pitch'] = data['pitch']
        data['roll'] = data['roll']
        data['yaw'] =  data['yaw']


        #print(data['roll'],data['pitch'],data['yaw'])
        # print(di)
    
        sock.SendData(json.dumps(data).encode('utf-8')) # Send this string to other application
    i += 2
    # lat +=3
    # lon +=3

    dat = sock.ReadReceivedData() # read data

    if dat != None: # if NEW data has been received since last ReadReceivedData function call
            print(type(dat)) # print new received data
            print(dat)
            dat = "{"+dat+"}"
            dat = json.loads(dat)

            # Convert NED Unreal Quaternion ENU Quaternion
            ned_w= float(dat["w"])
            ned_x = float(dat["x"])
            ned_y = float(dat["y"])
            ned_z = float(dat["z"])

            cam_final_quat = Quaternion(ned_w,ned_z,ned_x,ned_y)
            
            

            # cam_final_quat = cam_final_quat.rotate(Quaternion(vector=[0, 0, -1]))
            
            # cam_final_quat = ned_to_enu_quaternion([cam_final_quat.w, cam_final_quat.x,cam_final_quat.y , cam_final_quat.z ])




            c = GC.CameraRayProjection(69,[float(dat["lat"]),float(dat["lon"]),float(dat["alt"])],
                                       [int(float(dat["resw"])),int(float(dat["resh"]))],
                                    
                                       GC.Coordinates(int(float(dat["xpos"])),int(float(dat["ypos"]))),
                                    #    [float(unreal_enu_quat["w"]),float(unreal_enu_quat["x"]), float(unreal_enu_quat["y"]), float(unreal_enu_quat["z"])]
                                    [cam_final_quat[0],cam_final_quat[1],cam_final_quat[2],cam_final_quat[3]])
            target_direction_ENU = c.target_ENU()
            target_direction_ECEF = c.ENU_to_ECEF(target_direction_ENU)
            intersect_ECEF = c.target_location(target_direction_ECEF)
            
            intersect_LLA = c.ECEFtoLLA(intersect_ECEF.x,intersect_ECEF.y,intersect_ECEF.z)
            print(c.LLAtoXYZ(intersect_LLA[0], intersect_LLA[1], intersect_LLA[2]))
            print(intersect_LLA)
            
            di2 = {
                'lat' : str(intersect_LLA[0]),
                'lon' : str(intersect_LLA[1]),
                'alt' : str(intersect_LLA[2])
            }
            sock2.SendData(json.dumps(di2).encode('utf-8'))

    dat2 = sock2.ReadReceivedData() # read data
    #implement timer function
    if dat2 != None: # if NEW data has been received since last ReadReceiveddat function call
            print(type(dat)) # print new received dat
            print(dat)
            

    time.sleep(0.1)


