// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Geospatial;
using System;

namespace Microsoft.Maps.Unity
{
    /// <summary>
    /// A <see cref="MapScene"/> described by a <see cref="GeoBoundingBox"/> or <see cref="MercatorBoundingBox"/>.
    /// </summary>
    public class MapSceneOfBoundingBox : MapScene
    {
        /// <summary>
        /// The bounds of the MapScene.
        /// </summary>
        public GeoBoundingBox BoundingBox { get; private set; }

        /// <summary>
        /// Constructs a <see cref="MapSceneOfBoundingBox"/> from the specified <see cref="GeoBoundingBox"/>.
        /// </summary>
        public MapSceneOfBoundingBox(GeoBoundingBox boundingBox)
        {
            BoundingBox = boundingBox;
        }

        /// <summary>
        /// Constructs a <see cref="MapSceneOfBoundingBox"/> from the specified <see cref="MercatorBoundingBox"/>.
        /// </summary>
        public MapSceneOfBoundingBox(MercatorBoundingBox boundingBox) :
            this(boundingBox.ToGeoBoundingBox())
        {
        }

        /// <inheritdoc/>
        public override void GetLocationAndZoomLevel(MapRendererBase mapRenderer, out LatLon location, out double zoomLevel)
        {
            if (mapRenderer == null)
            {
                throw new ArgumentNullException(nameof(mapRenderer));
            }

            location = BoundingBox.Center;
            var localMapDimension = mapRenderer.LocalMapDimension;
            var targetMercatorBounds = BoundingBox.ToMercatorBoundingBox();
            var zoomLevelForXDimension = Math.Log(localMapDimension.x / targetMercatorBounds.Width, 2);
            var zoomLevelForYDimension = Math.Log(localMapDimension.y / targetMercatorBounds.Height, 2);
            zoomLevel = (float)Math.Min(zoomLevelForXDimension, zoomLevelForYDimension) + 1;
            zoomLevel = Math.Min(Math.Max(zoomLevel, MapConstants.MinimumZoomLevel), MapConstants.MaximumZoomLevel);
        }
    }
}
