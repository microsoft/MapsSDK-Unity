# Changelog
All notable changes to the SDK NuGet package and it's supporting scripts will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and the SDK NuGet package adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

Refer to the [Getting Started](https://github.com/microsoft/MapsSDK-Unity/wiki/Getting-started) documentation for instructions about how to import and upgrade the SDK.

## 0.7.1 - 2020-05-08

### Maps SDK
#### Fixed
- Meta files used for native plugins are now compatible with Unity 2018.4+. [#52](https://github.com/microsoft/MapsSDK-Unity/issues/52)
- Fixed bug that caused raycast to fail in some conditions.
- Reduced overhead of clipping distance render pass.

## 0.7.0 - 2020-05-04

### Maps SDK
#### Added
- Native plugin that improves the efficiency of decoding map data and signifcantly reduces GC usage. Supported platforms: Android/iOS/Windows.
- Compressed texture formats for model data on Android/iOS, reducing memory usage on these platforms.
- Elevation scale API that can be used to exaggerate or flatten terrain. [#50](https://github.com/microsoft/MapsSDK-Unity/issues/50)

#### Removed
- Removed support for the deprecated UWP .NET scripting backend.

## 0.6.1 - 2020-04-17

### Maps SDK
#### Changed
- Removed 257x257 dimension restriction of `ElevationTiles`.
#### Fixed
- Threading issue causing map data to not load. [(#40)](https://github.com/microsoft/MapsSDK-Unity/issues/40)
- Further reduced large temporary GC allocations on background threads.

### Supporting Scripts
#### Added
- `UnityWebRequestAwaiterExtensionMethods` to enable UnityWebRequests to be used with async/await.

## 0.6.0 - 2020-04-09
### Maps SDK
#### Added
- [`ElevationTileLayer`](https://github.com/microsoft/MapsSDK-Unity/wiki/ElevationTileLayer) to enable rendering custom elevation data sources.

#### Fixed
- More reduction of GC allocations. There are no longer allocations when the map is idle. 
- Copyright text not being culled and showing through map.
- Copyright text misaligned in certain rotations.


## 0.5.1 - 2020-03-03
### Maps SDK
#### Added
- [`TextureTile.FromUrl`](https://github.com/microsoft/MapsSDK-Unity/wiki/Microsoft.Maps.Unity#texturetile) API to simplify requesting images (JPEG/PNG) from the web. Custom implementations of `TextureTileLayer` will no longer need to interact with UnityWebRequest or HttpClient to request imagery.
- [`UnityTaskFactory`](https://github.com/microsoft/MapsSDK-Unity/wiki/Microsoft.Maps.Unity#unitytaskfactory) provides some utility APIs to run async tasks on Unity's main thread.

#### Changed
- `TextureTileLayer` API return type has been changed to a `Nullable<TextureTile>`. This is a breaking change if you have implemented a custom `TextureTileLayer`.

#### Fixed
- Empty textures on iOS caused by DXT1 texture format. Any DXT1 texture will now be transcoded to RGB24 (on iOS only). 
- Large reduction in overall size and frequency of GC allocations caused by the MapRenderer.

### Supporting Scripts
#### Changed
- `HttpTextureTileLayer` updated to leverage `TextureTile.FromUrl` API.

## 0.5.0 - 2020-01-08
### Maps SDK
#### Added
- Circular [`MapShape`](https://github.com/microsoft/MapsSDK-Unity/wiki/Configuring-the-MapRenderer#map-layout) option.

#### Changed
- New approach for rendering side walls of map. Does not rely on geometry shaders.

## 0.4.2 - 2019-12-11
### Maps SDK
#### Added
- The maximum cache size used for storing map data is now configurable. See the _Quality Settings_ section of the MapRenderer's editor. [(#35)](https://github.com/microsoft/MapsSDK-Unity/issues/35)

#### Changed
- The default MapRenderer cache size has been lowered. Currently the cache size uses 1/3 of system memory, but can be no larger than 2GB. The maximum size is now configurable. [(#35)](https://github.com/microsoft/MapsSDK-Unity/issues/35)

#### Fixed
- Intermittent exception message related to TextureTileLayers. [(#35)](https://github.com/microsoft/MapsSDK-Unity/issues/35)
- The MapRenderer now respects layer setting of the GameObject. MapPins and MapLabels automatically use same layer as parent MapRenderer. [(#32)](https://github.com/microsoft/MapsSDK-Unity/issues/32)

## 0.4.1 - 2019-11-15
### Maps SDK
#### Added
- The [`DefaultTextureTileLayer`](https://github.com/microsoft/MapsSDK-Unity/wiki/Configuring-the-MapRenderer#defaulttexturetilelayer) now supports Bing Maps symbolic imagery as well as aerial imagery with road and label overlays. Previously aerial imagery (without road overlays or labels) was the only imagery type supported.

### Supporting Scripts
#### Added
- [`HttpTextureTileLayer`](https://github.com/microsoft/MapsSDK-Unity/wiki/Customizing-map-textures#httptexturetilelayer) component that makes it easy to stream texture tiles by specifying a formatted URL.
- More Editor UI improvements around `TextureTileLayer`. Right-clicking a tile layer in list now brings up option to edit the underlying script.
#### Changed
- `MapRendererTransformExtension` APIs renamed to more closely follow Unity's transformation-related methods.

## 0.4.0 - 2019-10-23
### Maps SDK
#### Added
- `TextureTileLayer` class that can be extended to customize imagery used by the `MapRenderer`. Implementations can request data from other Web Mercator tile services, load texture data from disk, or generate textures on the fly. Multiple `TextureTileLayers` can be used and layered together, e.g. to overlay partially transparent textures like weather data.
- `DefaultTextureTileLayer` is added automatically to the `MapRenderer`. Pulls sattelite aerial imagery from Bing Maps. 
- Option to enable MRTK hoverlight functionality on the `MapRenderer` terrain.
#### Fixed
- Spammy error logging when key is invalid.
- Inaccurate results when raycasting elevation data.
- Inaccurate rendering of elevation data.

### Supporting Scripts
#### Added
- `MapContourLineLayer` renders lines of constant elevation. Line interval and color can be changed dynamically.
- Various editor scripts for modifying `TextureTileLayers`.

## 0.3.0 - 2019-08-26
### Maps SDK
#### Added
- Ability to increase or decrease the terrain quality of the `MapRenderer`.
- Ability to disable high-res 3D terrain models, i.e. only use height-map based elevation.
- Ability to render only a flat map surface by disabling aforementioned 3D terrain sources.
- In-editor mouse controls to drag the center of the `MapRenderer` and adjust it's zoom level. Similar mouse controls added for `MapPins` to allow for adjusting a pin's position relative to the map.
- `MapPinLayer` now supports serialization of it's children items, so `MapPins` can be added to the layer in the editor rather than strictly at runtime.
#### Changed
- Copyright settings for `MapRenderer` now managed by separate component, `MapCopyrightLayer`. This component is auto-added to `GameObjects` with a `MapRenderer` component.
#### Fixed
- Handle case when developer key isn't present more gracefully. Less console spam and `MapPins` will continue to position correctly.

### Supporting Scripts
#### Added
- Initial Bing Maps service API. Currently supports geocoding and reverse geocoding via the `LocationFinder`.
- Editor scripts for various components have been moved out of the DLL and are now available in the supporting scripts.
#### Changed
- Editor UI refresh for MapRenderer. Among other visual improvements, the developer key field acts as a password field and the value is now hidden by asterisks.

## 0.2.3 - 2019-07-15
### Maps SDK
#### Added
- `MapScene` animation-related logic has been modularized and is now customizable. Default implementation moved to supporting scripts.
- Refreshed editor UI for the `MapRenderer`.
#### Fixed
- Clamp boundary values for various ```MapRenderer``` properties like center and zoom level.

### Supporting Scripts
#### Added
- Method to calculate scale ratio between the map and Unity's world space.
- `DefaultAnimationController` implementation used for animating `MapScenes`. This implementation improves the animation speed to account for the logarithmic scaling of zoom level.

## 0.2.2 - 2019-06-12
### Maps SDK
#### Added
- Additional max distance parameter for the raycast API.
- Provide normal and distance in the raycast result.

### Supporting Scripts
#### Added
- Extension class to `MapRenderer` for transforming points between Unity's world and local coordinate spaces to the map's geographic coordinate system (latitude, longitude, and altitude).

## 0.2.1 - 2019-05-28
### Maps SDK
#### Fixed
- Rolled back elevation fallback logic which was based on BC1 texture support of the device. Unity is decoding BC1 to RGB automatically.

## 0.2.0 - 2019-05-24
### Maps SDK
#### Added
- Raycast API on `MapRenderer` to return hit point and corresponding LatLonAlt of a ray intersection with the map.
#### Fixed
- Perf improvement for rendering. Reduces number of vertices required in certain views.
- Map edge no longer disappears when the map surface is above the viewport.
- Android/iOS compatibility improvements: High-res meshes would render incorrectly on devices that do not support the BC1 texture format. If the device lacks BC1 texture support, fall back to elevation-only rendering instead, which uses the widely supported RGB texture format.

## 0.1.5 - 2019-05-07
### Maps SDK
#### Fixed
- Networking performance improvements.

## 0.1.4 - 2019-04-30
### Maps SDK
#### Fixed
- Copyright text is now positioned correctly when the map layout is not using the default dimensions.
- Cleaned up the ```ClusterMapPin``` editor UI to remove unnecessary properties.

## 0.1.3 - 2019-04-23
### Maps SDK
#### Fixed
- Regression in ```MapPin``` size when using real-world scale.

## 0.1.2 - 2019-04-22
### Maps SDK
#### Added
- Altitude can now be specified on MapPins.
- Maps SDK-related components will now have their help icons direct out to the relevant wiki doc page.

#### Fixed
- Custom Maps SDK component icons for the editor are now working again.
- `MapPins` childed to `MapRenderer` are positioned correctly in editor after script reloading.

## 0.1.1 - 2019-04-16
### Maps SDK
#### Fixed
- Elevation terrain tiles will now fall back to lower LODs correctly.
- Seams between tiles should now be much less visible.

## 0.1.0 - 2019-04-09
### Maps SDK
#### Added
- `MapRenderer` component that handles streaming and rendering of 3D terrain data.
- `MapPinLayer` that allows for positioning `GameObjects` on the ```MapRenderer``` at a specified LatLon.
- Ability to cluster `MapPins` per `MapPinLayer`. This allows for efficiently rendering large data sets.
- Ability to animate the position and zoom level of the map via the `SetMapScene` API.
- `MapLabelLayer` for displaying city labels.
- Support for shadow casting and receiving of the map.
- Option to use a custom material for terrain.

### Supporting Scripts
#### Added
- Shader for rendering terrain with support for shadows, heightmap offsets, and clipping to the `MapRenderer`'s dimensions.
- Shader for rendering side of map. Dynamically generates appropriate triangles by the geometry shader. Supports shadows.
- Script with helper functions to navigate the MapRenderer, e.g. panning and zooming.
- Script for animating map to the specified location and zoom level.
