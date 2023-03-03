
import UdpComms as U
import time
import cv2
import base64
import imutils

sock = U.UdpComms(udpIP="127.0.0.1", portTX=8000, portRX=8001, enableRX=True, suppressWarnings=True)
cam = cv2.VideoCapture(r'C:\Users\rushi\Desktop\Research\CrimeScene.mp4')

while True:
    ret,camImage = cam.read()
    frame = imutils.resize(camImage,width=400)
    encoded,buffer = cv2.imencode('.jpg',frame,[cv2.IMWRITE_JPEG_QUALITY,80])
    byteString = base64.b64encode(buffer)
    
    print('...')
    sock.SendData(byteString) 

    time.sleep(1)