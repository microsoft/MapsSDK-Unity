// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using Microsoft.Geospatial.VectorMath;
    using System;
    using UnityEngine;

    /// <summary>
    /// Helpers to transform between Unity's world and local spaces to the geographic coordinate system of the MapRenderer.
    /// </summary>
    public static class MapRendererTransformExtensions
    {
        private const double EquatorialCircumferenceInWgs84Meters = 40075016.685578488;

        /// <summary>
        /// Transforms an XYZ point in world space to a Mercator position.
        /// </summary>
        public static Vector2D TransformWorldToMercator(this MapRenderer mapRenderer, Vector3 worldSpacePoint)
        {
            var localSpacePoint = mapRenderer.transform.InverseTransformPoint(worldSpacePoint);
            return TransformLocalToMercator(mapRenderer, localSpacePoint);
        }

        /// <summary>
        /// Transforms an XYZ point in world space to a LatLon.
        /// </summary>
        public static LatLon TransformWorldToLatLon(this MapRenderer mapRenderer, Vector3 worldSpacePoint)
        {
            return new LatLon(mapRenderer.TransformWorldToMercator(worldSpacePoint));
        }

        /// <summary>
        /// Transforms an XYZ point in world space to a LatLonAlt.
        /// </summary>
        public static LatLonAlt TransformWorldToLatLonAlt(this MapRenderer mapRenderer, Vector3 worldSpacePoint)
        {
            var localSpacePoint = mapRenderer.transform.InverseTransformPoint(worldSpacePoint);
            localSpacePoint.y -= mapRenderer.LocalMapHeight;

            // Compute LatLon.
            var mercatorCoordinate = TransformLocalToMercator(mapRenderer, localSpacePoint);
            var latLon = new LatLon(mercatorCoordinate);

            // Compute elevation (an altitude in meters).
            var inverseMercatorScale = Math.Cos(latLon.LatitudeInRadians);
            var equatorialCircumferenceInLocalSpace = Math.Pow(2, mapRenderer.ZoomLevel - 1);
            var elevationScale = inverseMercatorScale * (EquatorialCircumferenceInWgs84Meters / equatorialCircumferenceInLocalSpace);
            var altitudeInMeters = localSpacePoint.y * elevationScale + mapRenderer.ElevationBaseline;

            return new LatLonAlt(ref latLon, altitudeInMeters);
        }

        /// <summary>
        /// Transforms an XYZ point in world space to a LatLonAlt.
        /// </summary>
        public static Vector3 TransformLatLonAltToWorld(this MapRenderer mapRenderer, LatLonAlt location)
        {
            // Can compute XZ coords in local space from the Mercator position and map center.
            var mercatorPosition = location.LatLon.ToMercatorPosition();
            var mercatorPositionRelativeToCenter = mercatorPosition - mapRenderer.Center.ToMercatorPosition();
            var equatorialCircumferenceInLocalSpace = Math.Pow(2, mapRenderer.ZoomLevel - 1);
            var localSpaceXZ = equatorialCircumferenceInLocalSpace * mercatorPositionRelativeToCenter;

            // Altitude to local y value.
            var offsetMapAltitudeInMeters = location.AltitudeInMeters - mapRenderer.ElevationBaseline;
            var mercatorScale = 1.0 / Math.Cos(location.LatLon.LatitudeInRadians);
            var altitudeInLocalSpace = offsetMapAltitudeInMeters * mercatorScale * (equatorialCircumferenceInLocalSpace / EquatorialCircumferenceInWgs84Meters);

            var localPoint =
                new Vector3(
                    (float)localSpaceXZ.X,
                    (float)(altitudeInLocalSpace + mapRenderer.LocalMapHeight),
                    (float)localSpaceXZ.Y);

            return mapRenderer.transform.TransformPoint(localPoint);
        }

        /// <summary>
        /// Transforms an XYZ point in the MapRenderer's local space to a Mercator position.
        /// </summary>
        private static Vector2D TransformLocalToMercator(this MapRenderer mapRenderer, Vector3 localSpacePoint)
        {
            // x = -0.5...0.5 (left-to-right/west-to-east), y = -0.5...0.5 (bottom-to-top/south-to-north)
            var normalizedSurfaceCoordinate =
                new Vector2(
                    localSpacePoint.x / mapRenderer.LocalMapDimension.x,
                    localSpacePoint.z / mapRenderer.LocalMapDimension.y);

            var mercatorBox = mapRenderer.Bounds.ToMercatorBoundingBox();
            var mercatorX =
                mercatorBox.BottomLeft.X + (0.5 + normalizedSurfaceCoordinate.x) * (mercatorBox.TopRight.X - mercatorBox.BottomLeft.X);
            var mercatorY =
                mercatorBox.BottomLeft.Y + (0.5 + normalizedSurfaceCoordinate.y) * (mercatorBox.TopRight.Y - mercatorBox.BottomLeft.Y);

            return new Vector2D(mercatorX, mercatorY);
        }
    }
}
