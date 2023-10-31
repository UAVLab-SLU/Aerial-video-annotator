# AerialARFramework
### Unity application to augment virtual content in aerial video.

This Unity project simulates drone movement within a virtual world, rendering live footage from a drone and allowing users to annotate points of interest (POIs). It integrates with a Python server to provide frame rendering and geolocation calculations.

Live drone footage is continuously presented to users through frames received via a UDP socket from the Python server. These frames are rendered using a RawImage on a Unity canvas. Concurrently, metadata from the Python server adjusts the Unity camera's position in the 3D space, mirroring the real-time movements of the drone.

One of the project's core features is the ability for users to annotate POIs on the live drone feed. When an annotation is made, a data request is sent to the Python server. The server, in turn, calculates the real-world geolocation of the annotation. Once computed, this geolocation is sent back to Unity, where virtual markers are augmented on the canvas, aligned with the respective 3D position.

To ensure data accessibility and sharing, the geolocation metadata is seamlessly pushed to a cloud-based real-time database.

