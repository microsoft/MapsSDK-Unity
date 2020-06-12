// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;

    /// <summary>
    /// Animates a MapRenderer to the specified <see cref="MapScene"/>.
    /// </summary>
    public interface IMapSceneAnimationController
    {
        /// <summary>
        /// Returns a yieldable object that can be used to wait for animation to complete.
        /// </summary>
        WaitForMapSceneAnimation YieldInstruction { get; }

        /// <summary>
        /// Initializes the controller to animate the specified <see cref="MapScene"/>.
        /// </summary>
        void Initialize(MapRendererBase mapRenderer, MapScene mapScene, float animationTimeScale, MapSceneAnimationKind mapSceneAnimationKind);

        /// <summary>
        /// Updates the zoom level and location for this frame of the animation.
        /// </summary>
        bool UpdateAnimation(float currentZoomLevel, LatLon currentLocation, out float zoomLevel, out LatLon location);
    }
}
