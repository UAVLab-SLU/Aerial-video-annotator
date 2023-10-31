# AerialARFramework
### Unity application to augment virtual content in aerial video.

This Unity project simulates drone movement within a virtual world, rendering live footage from a drone and allowing users to annotate points of interest (POIs). It integrates with a Python server to provide frame rendering and geolocation calculations.

Live drone footage is continuously presented to users through frames received via a UDP socket from the Python server. These frames are rendered using a RawImage on a Unity canvas. Concurrently, metadata from the Python server adjusts the Unity camera's position in the 3D space, mirroring the real-time movements of the drone.

One of the project's core features is the ability for users to annotate POIs on the live drone feed. When an annotation is made, a data request is sent to the Python server. The server, in turn, calculates the real-world geolocation of the annotation. Once computed, this geolocation is sent back to Unity, where virtual markers are augmented on the canvas, aligned with the respective 3D position.

To ensure data accessibility and sharing, the geolocation metadata is pushed to a cloud-based real-time database.


### Setup

1. Clone this repository
2. Download UnityHub
3. Open the project(Unity/uav) using UnityHub and follow instructions to install appropriate version.
4. Open the project after installation is done.


### Connectivity

This project contains Unity application and it cannot connect with drone directly.

Server can be found at [repo](https://github.com/UAVLab-SLU/detection-and-tracking)

Entire pipeline can be tested in local, inorder to connect with real drone or to stream video from different laptop, follow the instructions below.

**Local:** 

After setting up server, follow instructions to run the entire pipeline.(run server.py)

**Across different laptops/ Outdoor testing with Real drone:**

1. Connect the two laptops using an Ethernet cable.
2. On Laptop 1, go to network connection properties and set:
    IP Address: 10.0.0.1,
    Subnet Mask: 255.255.255.0
3. On Laptop 2, repeat the above step but set:
    IP Address: 10.0.0.2
4. In server.py script, adjust the IP address to point to the opposite laptop (e.g., if running the server on Laptop 1, set to 10.0.0.2).
5. Disable firewall settings on both laptops to allow for unrestricted communication.
6. If any applications request network access, ensure you grant the required permissions.





