// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using System;

    /// <summary>
    /// Helpers to provide a map scale relative to Unity's world space.
    /// </summary>
    public static class MapScaleRatioExtensions
    {
        private const double EquatorialCircumferenceInWgs84Meters = 40075016.685578488;

        /// <summary>
        /// Computes approximate scale of the map relative to Unity's world space, i.e. the number of real-world meters in the map
        /// per a single unit in Unity's world space. Uses the <see cref="MapRenderer"/>'s center as the reference location.
        /// </summary>
        /// <remarks>
        /// Scale can change based on the latitude of the <see cref="MapRenderer"/>'s location due to the Mercator projection.
        /// <br/>
        /// The resulting scale will be inaccurate if a non-uniform scaling is applied to the <see cref="MapRenderer"/>'s transform.
        /// </remarks>
        public static double ComputeUnityToMapScaleRatio(this MapRenderer mapRenderer)
        {
            if (mapRenderer == null)
            {
                throw new ArgumentNullException(nameof(mapRenderer));
            }

            return ComputeUnityToMapScaleRatio(mapRenderer, mapRenderer.Center);
        }

        /// <summary>
        /// Computes approximate scale of the map relative to Unity's world space, i.e. the number of real-world meters in the map
        /// per a single unit in Unity's world space.
        /// </summary>
        /// <remarks>
        /// The scale can change based on the latitude of the reference point due to distortion of the Mercator projection.
        /// <br/>
        /// The resulting scale will be inaccurate if a non-uniform scaling is applied to the <see cref="MapRenderer"/>'s transform.
        /// </remarks>
        public static double ComputeUnityToMapScaleRatio(this MapRenderer mapRenderer, LatLon referenceLocation)
        {
            if (mapRenderer == null)
            {
                throw new ArgumentNullException(nameof(mapRenderer));
            }

            var mapRendererScale = mapRenderer.transform.localScale.x;
            var totalMapWidthInWorldSpace = Math.Pow(2, mapRenderer.ZoomLevel - 1) * mapRendererScale;
            var inverseMercatorScale = Math.Cos(referenceLocation.LatitudeInRadians);
            var mapCircumferenceInWgs84Meters = EquatorialCircumferenceInWgs84Meters * inverseMercatorScale;
            return mapCircumferenceInWgs84Meters / totalMapWidthInWorldSpace;
        }
    }
}
