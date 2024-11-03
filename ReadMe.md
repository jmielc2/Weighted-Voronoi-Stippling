# Weighted Voronoi Stippler
Here is my implementation of the weighted voronoi stippling image post processing effect. There are two implementations of it. The first one is the slow implementation found in `Assets/Slow Stippler`. This implementation runs entirely on the CPU and, as the name suggests, works slowly. This was my proof of concept implementation that got the process down. The second implementation can be found at `Assets/Fast Stippler`. This implementation is faster since runs mostly on the GPU. NOTE: It is still a work in process!
## Results
<img width="572" alt="stipple-skull" src="https://github.com/user-attachments/assets/b8ee8bd7-8ca8-476e-a9bc-32bae77cf036">
<img width="878" alt="stipple-car" src="https://github.com/user-attachments/assets/2c84dc51-f653-438b-b282-001e37f73986">
<img width="560" alt="stipple-pumpkin" src="https://github.com/user-attachments/assets/c1b15ac1-cee5-4e02-ae3c-0c6ad6abb9cd">
