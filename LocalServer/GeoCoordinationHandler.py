#!/usr/bin/env python3
from dataclasses import dataclass
from typing import Tuple
import numpy as np
import transformations
from droneresponse_mathtools import Lla, Pvector
import paho.mqtt.client as mqtt
import json 
import time 

QuaternionTuple = Tuple[float, float, float, float] 
TOPIC = "geolocation/position"

#preliminary MQTT functions

def on_connect(client,userdata,flags,rc):
    if rc==0:
        print("Connected to broker")
        mqtt.Client.connected_flag = True
    else:
        print("Failed to connect, return code: ", rc)

def on_disconnect(client,userdata,rc):
    if rc != 0:
        print("Unexpected disconnect with error number {}".format(rc))
    client.loop_stop()

def get_mqtt_config():
    json_file = open('./settings.json')
    config_file = json.load(json_file)
    json_file.close()
    broker = config_file['mqtt_broker_address']
    port = config_file['mqtt_port']
    #"broker.emqx.io" to run locally (free broker)
    #print("Connecting to broker {} at port {}".format(broker,port))
    return {"broker":broker,"port":port}

def create_client():
    mqtt_config = get_mqtt_config()
    client = mqtt.Client()
    client.connected_flag = False
    client.on_connect = on_connect 
    client.on_message = on_message 
    client.on_disconnect = on_disconnect
    client.connect(mqtt_config["broker"],mqtt_config["port"])
    return client

def subscribe_to_lease_requests(client):
    client.subscribe(TOPIC)  # to listen to all subtopics of leasing 


def route_parser(message):
    json_request = json.loads(message) 
    FOVh = float(json_request["FOVh"])
    LLA = [float(i) for i in json_request["LLA"]]
    image_res = [int(i) for i in json_request["Image Res"]]
    target_coors = Coordinates(json_request["Target Coors"][0],json_request["Target Coors"][1])
    quaternion = [float(i) for i in json_request["Quaternion"]]
    return CameraRayProjection(FOVh,LLA,image_res,target_coors,quaternion)


#route request processing 
def on_message(client, userdata, message):
    c = route_parser(message.payload)
    target_direction_ENU = c.target_ENU()
    target_direction_ECEF = c.ENU_to_ECEF(target_direction_ENU)
    intersect_ECEF = c.target_location(target_direction_ECEF)
    #print("Intersect ECEF", intersect_ECEF.x,intersect_ECEF.y,intersect_ECEF.z)
    intersect_LLA = c.ECEFtoLLA(intersect_ECEF.x,intersect_ECEF.y,intersect_ECEF.z)
    print(intersect_LLA)
    
'''
X,Y camera coordinates
'''
class Coordinates:
    def __init__(self,x,y):
        self.x = float(x) 
        self.y = float(y) 

class Vector:
    '''
    Vector class to contain x,y,z coordinates representing a ray direction or point.
    '''
    def __init__(self,x,y,z):
        self.x = x 
        self.y = y
        self.z = z 
        
    def normalize(self):
        vector = np.array([self.x,self.y,self.z])
        mag = np.linalg.norm(vector)
        norm_vec=vector/mag
        return Vector(norm_vec[0],norm_vec[1],norm_vec[2])


'''
CameraRayProjection class - finds the target LLA location 

Algo:
-Receive a quaternion in ENU with the drone camera's current orientation
-Calculate the direction vector/camera ray through a particular pixel in the image taken
-Transform the camera ray projection to an ENU direction with respect to the drone
-Transform the camera ENU direction to an ECEF direction vector
-Define the drone's origin as its position in ECEF 
-Define the ground as the following: (drone's latitude, drones latitude, - (drone's altitude)) then convert it to ECEF
-Define the normal as (cosλ*cosφ,sinλ*cosφ,sinφ) with  φ = latitude, λ = longitude 
-Take the intersection of the ray to the plane, which is an ECEF point
-Convert the ECEF point of intersection to LLA 
'''


@dataclass
class CameraRayProjection:

    FOVh: float
    LLA: Tuple[float,float,float]
    image_res: Tuple[float,float]
    target_coors: Coordinates
    quaternion: QuaternionTuple

    def __init__(self,FOVh:float,LLA:Tuple,image_res:Tuple,target_coors:Coordinates,quaternion:QuaternionTuple):
            self.FOVh = FOVh
            self.LLA = LLA 
            self.latitude,self.longitude,self.altitude = self.LLA[0],self.LLA[1],self.LLA[2]
            self.image_width,self.image_height = image_res[0],image_res[1]
            self.xy_coors = self.raster_to_xy(target_coors,image_res)
            self.quaternion = quaternion
            
            #Constant Params
            self.K = self.k_factor()
            self.Alpha = self.alpha_angle()
            self.Beta = self.beta_angle()
        
    '''
    Generates the parameters used to calculate the camera ray projection through a pixel point.
    '''
    def k_factor(self):
        return (1/(2*np.tan(np.deg2rad(self.FOVh/2))))
  
    def alpha_angle(self):
        x_coors = self.xy_coors[0]
        return (np.arctan(x_coors/(self.image_width*self.K)))
    
    def beta_angle(self):
        x_coors,y_coors = self.xy_coors[0],self.xy_coors[1]
        return (np.arctan(y_coors/np.sqrt((self.K*self.image_width)**2+(x_coors)**2)))
    
    '''
    Params: QuaternionTuple containing information about the drone's current orientation in ENU.
    Return: Homogeneous rotation matrix for ENU.
    '''
    # def transformation_ENU(self)->np.array:
    #     R = transformations.quaternion_matrix(self.quaternion)
    #     new_array = np.array(R)
    #     new_array = np.delete(new_array,obj=3,axis=0)
    #     transformation_matrix = np.delete(new_array,obj=3,axis=1)
    #     return transformation_matrix
    
    def transformation_ENU(self)->np.array:
        R = transformations.quaternion_matrix(self.quaternion)
        new_array = np.array(R)
        new_array = np.delete(new_array,obj=3,axis=0)
        transformation_matrix_NED = np.delete(new_array,obj=3,axis=1)
        
        NED_to_ENU = np.array([[0, 1, 0], [1, 0, 0], [0, 0, -1]])
        transformation_matrix_ENU = NED_to_ENU @ transformation_matrix_NED

        # rot = np.array([[-1, 0, 0], [0, -1, 0], [0, 0, 1]])
        # # rotd = np.dot(transformation_matrix_ENU, rot)
        # rotd = rot @ transformation_matrix_ENU 

        return transformation_matrix_ENU
    

    '''
    Transformations from :
        - https://gssc.esa.int/navipedia/index.php/Transformations_between_ECEF_and_ENU_coordinates -
    Returns rotation matrix from ENU coordinates to ECEF.
    Constants:
        φ  = latitude 
        λ = longitude
    '''
    def transformation_ECEF(self)->np.array:
       

        Phi,Lambda = np.deg2rad(self.latitude), np.deg2rad(self.longitude)

        m00 = -np.sin(Lambda)
        m01 = -np.cos(Lambda)*np.sin(Phi)
        m02 = np.cos(Lambda)*np.cos(Phi)

        m10 = np.cos(Lambda)
        m11 = -np.sin(Lambda)*np.sin(Phi)
        m12 = np.sin(Lambda)*np.cos(Phi)

        m20 = 0
        m21 = np.cos(Phi)
        m22 = np.sin(Phi)
        
        rotation_matrix = np.array([[m00, m01, m02], [m10, m11, m12], [m20, m21, m22]])
        return rotation_matrix

    '''
    Params: Raster pixel coordinates, with the top left of the image as 0,0. Columns numered left to right beginning at 0, rows are top to bottom beginning with 0.
    Return: Distance in X and Y in pixels with respect to image center. X ∈ (-w/2,w/2).
    '''
    def raster_to_xy(self,raster_coors: Coordinates,image_res:Tuple)-> Tuple[float,float]:
        center_x,center_y = image_res[0]/2,image_res[1]/2
        x, y = raster_coors.x,raster_coors.y
        new_x = (x-center_x if x >= center_x else -(center_x-x))
        new_y = (center_y - y if y <= center_y else -(y-center_y))
        image_coors = Coordinates(new_x,new_y)
        return (image_coors.x*-1,image_coors.y*-1)

    '''
    Return: Column Vector of pixel direction from camera origin.
    '''
    def pixel_projection(self)->np.array:
        Vx = np.cos(self.Beta)*np.cos(self.Alpha)
        Vy = -np.cos(self.Beta)*np.sin(self.Alpha)
        Vz = np.sin(self.Beta)
        column_vector = np.array([[Vx], [Vy], [Vz]])
        return column_vector
    

    '''
    Params: Pixel projection (dv of pixel from camera frame), rotation matrix.
    Return: Vector from camera to target in ENU
    '''         
    def target_ENU(self):
        column_vector = self.pixel_projection()
        rotation_matrix = self.transformation_ENU()
        r1 = rotation_matrix.dot(column_vector)
        V_p = Vector(r1[0,0],r1[1,0],r1[2,0])
        return V_p
    
    '''
    Params: Vector from camera to target in ENU 
    Return: Vector from camera to target in ECEF 
    '''
    def ENU_to_ECEF(self,target_ENU_vector)->np.array:
        V_p = np.array([[target_ENU_vector.x],[target_ENU_vector.y],[target_ENU_vector.z]])
        ECEF_rotation_matrix = self.transformation_ECEF()
        r1 = ECEF_rotation_matrix.dot(V_p)
        V_ECEF = Vector(r1[0,0],r1[1,0],r1[2,0])
        return V_ECEF

    '''
    Params: Latitude, Longitude, Altitude with height in meters above ellipsoid
    '''
    @staticmethod
    def LLAtoXYZ (latitude, longitude, altitude)->Pvector:
        location = Lla(latitude,longitude,altitude)
        pvec = location.to_pvector()
        x,y,z = float(pvec.x), float(pvec.y), float(pvec.z)
        return x,y,z 
    '''
    Params: X,Y,Z in ECEF (meters)
    '''
    @staticmethod
    def ECEFtoLLA(x,y,z)->Lla:
        location = Pvector(x,y,z)
        lla_pos = location.to_lla()
        latitude, longitude, altitude =  lla_pos.latitude,lla_pos.longitude,lla_pos.altitude
        return (latitude, longitude, altitude)

        '''
        -Algorithm from Practical Geometry Algorithms: with C++ Code by Daniel Sunday-

        Finds the intersection of each of the 4 rays projecting from the camera to the ground.
        To change the plane/ground intersection, adjust plane_point.
        The normal for the horizontal plane always points upward at z = 1 in XYZ.
        In ECEF, the normal unit vector is equivalent to (cosλ*cosφ,sinλ*cosφ,sinφ).
        
        Params: Vector target_ENU -> direction of target in ENU 
        Return: Intersection point ENU 
        
        '''
    def target_location(self,target_ECEF)->Vector:
        Phi,Lambda = np.deg2rad(self.latitude), np.deg2rad(self.longitude)
        ECEF_drone = self.LLAtoXYZ(self.latitude,self.longitude,self.altitude)

        ECEF_drone = Vector(ECEF_drone[0],ECEF_drone[1],ECEF_drone[2])
        

        #drone origin ECEF is ray point 
        ray_point = np.array([ECEF_drone.x,ECEF_drone.y,ECEF_drone.z])
        #ray_direction is the direction vector to target in ECEF 
        ray_direction = np.array([target_ECEF.x,target_ECEF.y,target_ECEF.z])
        # position of ground in ECEF
        ECEF_ground = self.LLAtoXYZ(self.latitude,self.longitude,0)

        plane_point = np.array([ECEF_ground[0],ECEF_ground[1],ECEF_ground[2]])
        
        #for ground plane, "up" direction  
        plane_normal = np.array([(np.cos(Lambda)*np.cos(Phi)),(np.sin(Lambda)*np.cos(Phi)),np.sin(Phi)])

        denom = plane_normal.dot(ray_direction)
        dir_vector = ray_point - plane_point
        alpha = (-plane_normal.dot(dir_vector))/denom
        intersect = dir_vector + (alpha * ray_direction) + plane_point
        
        return Vector(intersect[0],intersect[1],intersect[2])
    

if __name__ == "__main__":
    # Pass in:
    #     FOVH in degrees, LLA (height in meters above ellipsoid), Image Res (wxh) in pixels, 
    #     Raster Coordinates of target pixel from bounding box (x,y) 
    #     Quaternion of camera position ENU.

    c = CameraRayProjection(2,[41.714911, -86.242250,200],[1080,200],Coordinates(350,200),[0.06146124, 0, 0, 0.99810947])
    target_direction_ENU = c.target_ENU()
    target_direction_ECEF = c.ENU_to_ECEF(target_direction_ENU)
    intersect_ECEF = c.target_location(target_direction_ECEF)
    #print("Intersect ECEF", intersect_ECEF.x,intersect_ECEF.y,intersect_ECEF.z)
    intersect_LLA = c.ECEFtoLLA(intersect_ECEF.x,intersect_ECEF.y,intersect_ECEF.z)
    print(intersect_LLA)



# def main():
#     global client 
#     client = create_client() # create and connect to the MQTT broker
#     subscribe_to_lease_requests(client)
#     client.loop_forever()  # Keep the loop running until receiving the on_disconnent event

#     while not client.connected_flag:
#         print("Waiting to connect")
#         time.sleep(1)

# if __name__ == '__main__':
#     main()


'''
coordinates: 41.687970, -86.249996, 100
ground: 41.687970, -86.249996, 0 
target_enu: [0,np.sqrt(2)/2,-np.sqrt(2)/2]


'''
