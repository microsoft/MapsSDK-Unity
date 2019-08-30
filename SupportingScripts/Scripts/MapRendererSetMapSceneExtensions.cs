// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    /// <summary>
    /// Extension method that allows for setting MapScene using the default MapSceneAnimationController.
    /// </summary>
    public static class MapRendererSetMapSceneExtensions
    {
        /// <summary>
        /// Sets the MapRenderer's view to reflect the new MapScene using the default IMapSceneAnimationController.
        /// </summary>
        /// <returns>
        /// A yieldable object is returned that can be used to wait for the end of the animation in a coroutine.
        /// </returns>
        public static WaitForMapSceneAnimation SetMapScene(
            this MapRenderer mapRenderer,
            MapScene mapScene,
            MapSceneAnimationKind mapSceneAnimationKind = MapSceneAnimationKind.Bow,
            float animationTimeScale = 1.0f)
        {
            return mapRenderer.SetMapScene(
                mapScene,
                new MapSceneAnimationController(),
                mapSceneAnimationKind,
                animationTimeScale);
        }
    }
}
