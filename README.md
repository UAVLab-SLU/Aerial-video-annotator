# AerialARFramework
Unity application to augment virtual content in aerial video frames.

This unity project is to simulate drone movement in a virtual world and calculate geolocation of user click. Virtual objects can be augmented at Points of interest.
Frames and metadata is received through a UDP socket from a python server. Frames are rendered continously by using a RawImage in Unity canvas and metadata is used to move camera in unity 3d space. Users can watch the live drone feed and annotate the Points of interest by using UI. GeoLocation of the annotations are calcuated by using a python script. When user marks an annotation, data is sent back to python server. Python server calculates the geolocation and sends back the data. Virtual markers are augmented on the Unity canvas at calculated geoLocation in unity 3d space.
GeoLocation metadata is pushed to cloud Realtime database to share it across systems. 
Python server and Unity application cannot run in a same computer as python server is connected to drone using Wifi and Unity application cannot access internet. To overcome this, Unity application is run in a different machione and we are using ethernet cable to connect python server and send data using UDP sockets.

