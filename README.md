# UWS - Universal Water System
Welcome to the Universal Water System (UWS) repository, a Unity plugin designed to add water surfaces such as oceans, rivers, and land using spline union and intersection. This system is inspired by the water system in Unreal Engine, offering powerful tools and flexible implementations for different platforms.

![awspic](https://github.com/anunknowperson/uws/assets/54563399/58a53e05-9bfe-4c6a-9240-cd5d84528d5a)


# Features
- Oceans, Rivers, and Land: Easily create and customize various water bodies using a robust spline-based system.
- Simple underwater effects.
- Planar reflections with addition camera.
- Realtime QuadTree LOD.
- User-Friendly Tools: Intuitive tools within the Unity editor for seamless integration and customization.
- Shaders for various targets: Setup once and run across various platforms.

# Platform Implementations
- Mobile:
    - Flat shader with moving texture and static low-poly meshes.

- Mid-Range:
    - Gerstner wave-based shader.
    - Real-time multithreaded meshing.
    - Reflections and refraction effects.

- High-Performance PCs:
    - Real-time multithreaded quad tree-based mesh detailing.
    - Realistic reflections using a second camera.
    - Refraction effects.
    - Realistic depth rendering.
    - FFT-based wave simulation.

# Project Status
This project is not production-ready and is currently incomplete!

# What You Can Do

- Fix Bugs and Optimize Performance: Adapt and customize the system to fit your needs.
- Utilize or Steal Code: Feel free to extract shaders or specific parts of the code, such as meshing or spline construction, for your projects.

# Known problems
- Divergence of edges in water surfaces
- Problems with filtering on High End shader

# Getting Started
To get started with UWS, create UWS folder in your project Assets folder and clone repository content into it.

Unity version: 2020.3.30f1

Sample project: https://github.com/anunknowperson/uws-demo/

# Contributing
Contributions are welcome! If you find bugs, want to optimize the system, or add new features, please fork the repository and submit a pull request.

# License
This project is licensed under the Apache 2.0 License. See the LICENSE file for details.

# Contact
For any questions or feedback, please open an issue on GitHub or reach out to me (discord: maestro8209).

# Credits:
- GLSL FFT implementation: https://github.com/jbouny/fft-ocean/tree/master
- Clipper C#
- Poly2Tri
