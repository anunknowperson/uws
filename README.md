# UWS - Universal Water System
Welcome to the Universal Water System (UWS) repository, a Unity plugin designed to add water surfaces such as oceans, rivers, and land using spline union and intersection. This system is inspired by the water system in Unreal Engine, offering powerful tools and flexible implementations for different platforms. Unfortunately, it's a bit broken tho...

![awspic](https://github.com/anunknowperson/uws/assets/54563399/8a31832b-c8d4-4cc6-aca2-4b6aabb54c5c)


# Features
- Oceans, Rivers, Lakes, Islands and Pools: Easily create and customize various water bodies using a robust spline-based system.
- Simple underwater effects.
- Planar reflections with addition camera.
- Cubemap Reflections.
- Light Reflection.
- Realtime QuadTree LOD for big worlds.
- Automatic flowmap generation.
- Manual flowmap editing on all water surfaces.
- Water decals - Put foam or random pictures over water surface! Decals can blend and also override water surface height.
- Automatic height decals placement on the beginnings and ends of the rivers.
- Physical buoyancy and objects drifting in river flow.
- User-Friendly Tools: Intuitive tools within the Unity editor for seamless integration and customization.
- Shaders for various targets: Setup once and run across various platforms.

# Spline system
- Ocean - The primary spline, beyond which the ocean will be rendered, and inside which nothing will be rendered. All other splines, except for the Pool and Island, must be located within the Ocean.
- Lake - Within this spline, there is a body of water inside the Ocean.
- Island - Within this spline, there will be no water inside the Lake/River/Ocean.
- River - Water within the Ocean that flows according to an auto-generated flowmap.
- Pool - Water within this spline that is independent of Lakes, Rivers, and Islands.

Within an Island, there can be a Lake, which may contain another Island, and so on...

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
- Divergence of edges in water surfaces.
- Problems with filtering on High End shader.
- Overall, there are some problems with shaders visual appereance.
- There can be crashes on unsupported settings values. (easy)
- Reflection Camera spam on each scene reopen in editor. (easy)
- For some reason, flowmap editing button is overriden by some editor actions. (easy)

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
