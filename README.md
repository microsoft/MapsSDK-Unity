<img src="https://github.com/Microsoft/MapsSDK-Unity/wiki/Content/Banner.png">

> The SDK is currently in preview: Feature requests and bug reports are very much welcome at the [issues page](https://github.com/Microsoft/MapsSDK-Unity/issues).
>
> We want to hear what features you want in order to accomplish mixed reality mapping scenarios!

# Overview
**Maps SDK, a Microsoft Garage project** provides a control to visualize a 3D map in Unity. The map control handles streaming and rendering of 3D terrain data with world-wide coverage. Select cities are rendered at a very high level of detail. Data is provided by Bing Maps.

The map control has been optimized for mixed reality applications and devices including the HoloLens, HoloLens 2, Windows Immersive headsets, HTC Vive, and Oculus Rift. Soon the SDK will also be provided as an extension to the [Mixed Reality Toolkit (MRTK)](https://github.com/Microsoft/MixedRealityToolkit-Unity).

| <img src="https://github.com/Microsoft/MapsSDK-Unity/wiki/Content/BoulderBalloon.gif"> | <img src="https://github.com/Microsoft/MapsSDK-Unity/wiki/Content/WeatherCube.gif"> | <img src="https://github.com/Microsoft/MapsSDK-Unity/wiki/Content/MtFujiZoom.gif">
| :--- | :--- | :--- |

# Getting started

For instructions to download and setup the control, check out the [Getting Started](https://github.com/Microsoft/MapsSDK-Unity/wiki/Getting-Started) page on the wiki.

The wiki also contains documentation, an API reference, and an in-depth overview of the sample scene.

# What is in this repo?

The core source code for the control is not part of this repository. The binary is available via a [NuGet package](https://www.nuget.org/packages/Microsoft.Maps.Unity). See [Microsoft® Bing™ Map Platform APIs Terms of Use](https://www.microsoft.com/maps/product/terms.html) for information about the terms of use.

This repository includes **samples**, **documentation** and **supporting scripts**.

The **supporting scripts** are Unity C# scripts that extend or build on-top of the SDK. Because of their usefulness, these supporting scripts are also included in the NuGet package. The version of the supporting scripts in the repository reflects the latest version of the scripts in the NuGet package.

Contributions to the supporting scripts are welcome, and approved changes will be folded back into the NuGet package. Refer to the [Contribution Process](CONTRIBUTING.md) for more details.

