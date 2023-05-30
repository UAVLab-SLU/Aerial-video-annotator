# Created by Youssef Elashry to allow two-way communication between Python3 and Unity to send and receive strings

# Feel free to use this in your individual or commercial projects BUT make sure to reference me as: Two-way communication between Python 3 and Unity (C#) - Y. T. Elashry
# It would be appreciated if you send me how you have used this in your projects (e.g. Machine Learning) at youssef.elashry@gmail.com

# Use at your own risk
# Use under the Apache License 2.0

# Example of a Python UDP server

import UdpComms as U
import time
import cv2
import base64
import imutils
# Create UDP socket to use for sending (and receiving)
sock = U.UdpComms(udpIP="127.0.0.1", portTX=8000, portRX=8001, enableRX=True, suppressWarnings=True)
cam = cv2.VideoCapture('CrimeScene.mp4')
i = 1

while True:
    ret,camImage = cam.read()

    frame = imutils.resize(camImage,width=400)
    encoded,buffer = cv2.imencode('.jpg',frame,[cv2.IMWRITE_JPEG_QUALITY,80])
    byteString = base64.b64encode(buffer)

    frameBytes = buffer.tobytes()
    
    # byteString = bytes(cv2.imencode('.jpg', buffer)[1].tostring())
    print('...',i)
    sock.SendData(frameBytes) # Send this string to other application
    i += 1

    data = sock.ReadReceivedData() # read data

    if data != None: # if NEW data has been received since last ReadReceivedData function call
        print(data) # print new received data

    # time.sleep(0.1)