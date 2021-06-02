# Changelog
All notable changes to the SDK NuGet package and it's supporting scripts will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and the SDK NuGet package adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

Refer to the [Getting Started](https://github.com/microsoft/MapsSDK-Unity/wiki/Getting-started) documentation for instructions about how to import and upgrade the SDK.

## 0.11.0 - 2021-06-02
### Maps SDK
#### Changed
- The clipping volume now requires a dedicated layer for rendering. The layer is set to 23 by default and can be changed via the MapRenderer inspector.
### Supporting Scripts
#### Changed
- Various modifications to the cginc files to make code compatible with SRP HLSL shaders.

## 0.10.2 - 2021-04-22

### Maps SDK
#### Added
 - Quality option to disable pre-caching of data around the current map.
 - Quality slider for elevation level of detail.
 - Classes for working with the WGS84: [`WGS84Datum`](https://github.com/microsoft/MapsSDK-Unity/wiki/Microsoft.Geospatial#wgs84datum), [`Vector3D`](https://github.com/microsoft/MapsSDK-Unity/wiki/Microsoft.Geospatial.VectorMath#vector3d)
#### Fixed
 - IL2CPP build errors in 2020.3. [#108](https://github.com/microsoft/MapsSDK-Unity/issues/108)
### Supporting Scripts
#### Added
 - Linear map animation with smoothing at the start and end.
 - Option to always show a `MapPin` even when it's outside the map's bounds.
 - In scene view, Ctrl+Right-click on `MapRenderer` now provides a context menu to add a `MapPin` at the selected location.
#### Changed
 - Removed unused parameter in FilterNormals shader function.
#### Fixed
 - Editor UI now handles type load exception. [#113](https://github.com/microsoft/MapsSDK-Unity/issues/113)

## 0.10.1 - 2020-12-11

### Maps SDK
#### Added
 - `MapRenderer.ApplyClippingVolumePropertiesToMaterial` to allow synchronizing the values used to clip objects to the map bounds in a custom shader.
 
### Supporting Scripts
#### Added
 - Overloads for `HttpTextureTileLayer`'s URL format placeholders. "Zoom level" can be specified using `{z}`, `{zoom}`, or `{zoomLevel}`.
#### Changed
 - `HttpTextureTileLayer`'s URL format placeholders are now case-insensitive.
#### Fixed
 - GC usage reduced in `HttpTextureTileLayer` requests.
 
## 0.10.0 - 2020-10-14

### Maps SDK
#### Added
 - Enhanced 3D coverage for various cities in Japan.
 - Various code APIs for getting and setting render-related properties, e.g. terrain material, shadow casting, etc.
#### Fixed
 - Reduced runtime GC allocations.
 
### Supporting Scripts
#### Added
 - New `MapInteractionHandler` components to handle touch and mouse interaction like pan, zoom, etc. Custom implementations can be derived for different input types. 

## 0.9.2 - 2020-08-12

### Maps SDK
#### Added
 - `IsLoaded` property on `MapRendererBase` to detect when map has completed loading all data for the current view. [#31](https://github.com/microsoft/MapsSDK-Unity/issues/31)
 - `DefaultTrafficTextureTileLayer` to visualize traffic flow.
#### Changed
 - The language used for localiztaion of map content is now a property on the `MapSession`.
#### Fixed
 - Internal exception that prevents map from loading 2020.1. [#86](https://github.com/microsoft/MapsSDK-Unity/issues/86)
 
### Supporting Scripts
#### Added
 - `WaitForMapLoaded` class to that can be yielded until the map has completed loading all data for the current view. [#31](https://github.com/microsoft/MapsSDK-Unity/issues/31)
 - Subdomain variable for `HttpTextureTileLayer`. [#70](https://github.com/microsoft/MapsSDK-Unity/issues/70)
#### Changed
- Increased MRTK hover light count in standard terrain shaders from 1 to 2.
#### Fixed
- Mitigate side wall rendering glitch in `MapShape.Block` mode. [#72](https://github.com/microsoft/MapsSDK-Unity/issues/72)

## 0.9.1 - 2020-06-18
### Supporting Scripts
#### Added
 - Remaining types of `MapRenderer` transformations: `TransformMercatorWithAltitudeToLocalPoint`, `TransformMercatorWithAltitudeToWorldPoint`, `TransformLatLonAltToLocalPoint`
####  Changed
 - Renamed `MapNavigation` to `MapInteractionController`. Added APIs for panning and zooming to a specified coordinate/ray.
#### Fixed
 - `MapPin` location could not be changed once initialized. [#66](https://github.com/microsoft/MapsSDK-Unity/issues/66)
 - `MapLabel` scaling was incorrect on first frame. [#68](https://github.com/microsoft/MapsSDK-Unity/issues/68)

## 0.9.0 - 2020-06-11
This release consists of a large number of breaking changes. Various components have been moved out of the core DLL to the Supporting Scripts. See the [Getting Started](https://github.com/microsoft/MapsSDK-Unity/wiki/Getting-started#migrating-to-090) page for information on how to migrate from previous versions.

### Maps SDK
#### Added
 - `MapSession` component manages developer key for associated `MapRenderer`s and map services.
 - `MapRendererBase` provides an abstract base class component which includes the core map rendering functionality.
 - `IPinnable` interface is used for types that need to be geospatially anchored to the map, e.g. `MapPin`, `ClusterMapPin`.
#### Changed
 - `MercatorBoundingCircle` moved to Microsoft.Geospatial namespace.
 - `MapRendererBase.LocalMapHeight` now represents the total height extent of the map, which varies based on content.
 - `MapRendererBase.LocalMapBaseHeight` provides the height offset where map content begins rendering. 
 - `MapRendererBase.Language` property can be changed at runtime.
#### Removed
 - `MapRenderer`, `MapPinLayer`, `MapPin`, `ClusterMapPin`, `MapPinSpatialIndex`, `MapDataCache`. These classes, or portions of, have been moved to the Supporting Scripts.
 - Removed `Vector2D`. Use `MercatorCoordinate` instead.
 - Removed `LatLon.FromMercatorPosition`. Use `MercatorCoordinate.ToLatLonAlt` instead.
 
### Supporting Scripts
#### Added
 - `MapRenderer` component. Extends from `MapRendererBase`. Handles tracking children `MapPin`s, managing the extents of the `MapCollider`, and managing map animations.
 - `MapPin` related scripts and implementation for indexing and clustering.
 - `MapScene` related scripts for animations.
 - `MapDataCache` determines cache size to use. On Android, this uses platform specific APIs to find a cache size that accounts for the application's memory limit. This class could be extended with improved implementations for other platforms.
 - Editor utlity to help migrate old component GUIDs to the updated values. The option is in `Assets -> Maps SDK for Unity -> Upgrade Component GUIDs`.
#### Fixed
 - Fixed bug preventing correct results from `TransformWorldPointToLatLonAlt` when the map object had scaling, translation, or rotation.
 - Fixed related issue where `MapRendererBase.ElevationScale` was not being applied to the transformations.
 
## 0.8.1 - 2020-05-27

### Maps SDK
#### Fixed
 - Adds dependencies to fix linker error when building iOS applications.

## 0.8.0 - 2020-05-20

### Maps SDK
#### Added
 - Localization improvements. The `MapRenderer` automatically detects device language and culture settings to localize map content. See the _Localization_ section of the MapRenderer's settings.
 - Helper class to convert Unity `SystemLanguage` enumerations to LCIDs or culture codes.
 - Ability to modify or disable the collider being used for rough collisions.
 
#### Fixed
 - Enabled options in the `DefaultTextureTileLayer` to provide raster imagery with roads, borders, and labels. [#34](https://github.com/microsoft/MapsSDK-Unity/issues/34)
 
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
