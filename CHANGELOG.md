# Changelog
All notable changes to the SDK NuGet package and it's supporting scripts will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and the SDK NuGet package adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## 0.1.1 - 2019-04-16 
### Maps SDK
#### Fixed
- Elevation terrain tiles now fall back to lower LODs correctly
- Seams between tiles should now be much less visible

## 0.1.0 - 2019-04-09

### Maps SDK
#### Added
- MapRenderer component that handles streaming and rendering of 3D terrain data.
- MapPinLayer that allows for positioning GameObjects on the MapRenderer at a specified LatLon.
- Ability to cluster MapPins per MapPinLayer. This allows for efficiently rendering large data sets.
- Ability to animate the position and zoom level of the map via the SetMapScene API.
- MapLabelLayer for displaying city labels.
- Support for shadow casting and recieving of the map.
- Option to use a custom material for terrain.

### Supporting Scripts
#### Added
- Shader for rendering terrain with support for shadows, heightmap offsets, and clipping to the MapRenderer's dimensions.
- Shader for rendering side of map. Dynamically generates appropriate triangles by the geometry shader. Supports shadows.
- Script with helper functions to navigate the MapRenderer, e.g. panning and zooming.
- Script for animating map to the specified location and zoom level.
