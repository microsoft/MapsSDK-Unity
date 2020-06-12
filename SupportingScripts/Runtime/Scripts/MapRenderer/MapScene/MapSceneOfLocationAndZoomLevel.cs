// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;

    /// <summary>
    /// A basic <see cref="MapScene"/> that can be used to change the map's location and zoom level.
    /// </summary>
    public class MapSceneOfLocationAndZoomLevel : MapScene
    {
        /// <summary>
        /// The final location.
        /// </summary>
        public LatLon Location { get; }

        /// <summary>
        /// The final zoom level.
        /// </summary>
        public float ZoomLevel { get; }

        /// <summary>
        /// Constructs a <see cref="MapSceneOfLocationAndZoomLevel"/> from the specified location and zoom level.
        /// </summary>
        public MapSceneOfLocationAndZoomLevel(LatLon location, float zoomLevel)
        {
            Location = location;
            ZoomLevel = zoomLevel;
        }

        /// <inheritdoc/>
        public override void GetLocationAndZoomLevel(out LatLon location, out double zoomLevel)
        {
            location = Location;
            zoomLevel = ZoomLevel;
        }
    }
}
