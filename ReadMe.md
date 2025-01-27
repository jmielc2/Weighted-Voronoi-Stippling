# Weighted Voronoi Stippler

This is my implementation of the weighted voronoi stippling image post processing effect. There are two implementations of it. The first one is the slower, CPU based implementation found in `Assets/CPU Stippler`. As the name suggests, this implementation works slowly since it doesn't take advantage of parallelization to process the image data. This was my proof of concept implementation that got the algorithm down. The second, GPU based implementation can be found at `Assets/GPU Stippler`. This implementation is faster since the image processing happens entirely on the GPU. Unfortunately, neither implementation is fast enough to run in a real-time contexts as it currently can only manage about 33 FPS with 5000 stipples, and that's without any additional rendering or processing. The bottleneck is the centroid calculation algorithm doesn't work nearly as fast enough as I'd like it to.

## Results

<img width="572" alt="stipple-skull" src="https://github.com/user-attachments/assets/b8ee8bd7-8ca8-476e-a9bc-32bae77cf036">
<img width="878" alt="stipple-car" src="https://github.com/user-attachments/assets/2c84dc51-f653-438b-b282-001e37f73986">
<img width="560" alt="stipple-pumpkin" src="https://github.com/user-attachments/assets/c1b15ac1-cee5-4e02-ae3c-0c6ad6abb9cd">

## References

- *Fast Computation of Generalized Voronoi Diagrams Using Graphics Hardware* by Kenneth E. Hoff III, Tim Culver, John Keyser, Ming Lin, Dinesh Manocha
- *Weighted Voronoi Stippling* by Adrian Secord
Referenced papers can be found in the `Documents` folder.