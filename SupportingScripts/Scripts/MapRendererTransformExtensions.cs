// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using Microsoft.Geospatial.VectorMath;
    using System;
    using UnityEngine;

    /// <summary>
    /// Helpers to transform between Unity's world and local spaces to the geographic coordinate system of the <see cref="MapRenderer"/>.
    /// </summary>
    public static class MapRendererTransformExtensions
    {
        /// <summary>
        /// The WGS84 ellipsoid circumference measured in meters.
        /// </summary>
        public const double EquatorialCircumferenceInWgs84Meters = 40075016.685578488;

        /// <summary>
        /// Constat for 2 * Math.PI.
        /// </summary>
        public const double TwoPi = 2 * Math.PI;

        /// <summary>
        /// Transforms an XYZ point in the <see cref="MapRenderer"/>'s local space to a Mercator position.
        /// </summary>
        public static Vector2D TransformLocalPointToMercator(this MapRenderer mapRenderer, Vector3 pointInLocalSpace)
        {
            var deltaFromMapCenterToPointInMercatorSpace = TransformLocalDirectionToMercator(mapRenderer, pointInLocalSpace);
            return mapRenderer.Center.ToMercatorPosition() + deltaFromMapCenterToPointInMercatorSpace;
        }

        /// <summary>
        /// Transforms an XYZ point in the <see cref="MapRenderer"/>'s local space to a Mercator position. Includes the altitude
        /// measured as meters from the WGS84 ellipsoid.
        /// </summary>
        public static Vector2D TransformLocalPointToMercatorWithAltitude(
            this MapRenderer mapRenderer,
            Vector3 pointInLocalSpace,
            out double altitudeInMeters,
            out double mercatorScale)
        {
            var mercatorPosition = TransformLocalPointToMercator(mapRenderer, pointInLocalSpace);

            mercatorScale = Math.Cosh(TwoPi * mercatorPosition.Y);
            var equatorialCircumferenceInLocalSpace = Math.Pow(2, mapRenderer.ZoomLevel - 1);
            var elevationScale = (EquatorialCircumferenceInWgs84Meters / equatorialCircumferenceInLocalSpace) / mercatorScale;
            altitudeInMeters = (pointInLocalSpace.y - mapRenderer.LocalMapHeight) * elevationScale + mapRenderer.ElevationBaseline;

            return mercatorPosition;
        }

        /// <summary>
        /// Transforms an XYZ direction in the <see cref="MapRenderer"/>'s local space to a direction in Mercator space.
        /// </summary>
        public static Vector2D TransformLocalDirectionToMercator(this MapRenderer mapRenderer, Vector3 directionInLocalSpace)
        {
            return TransformLocalDirectionToMercator(directionInLocalSpace, mapRenderer.ZoomLevel);
        }

        /// <summary>
        /// Transforms an XYZ direction in the <see cref="MapRenderer"/>'s local space to a direction in Mercator space.
        /// Uses the specified zoom level rather than the <see cref="MapRenderer"/>'s zoom level for the transformation.
        /// </summary>
        public static Vector2D TransformLocalDirectionToMercator(Vector3 directionInLocalSpace, double zoomLevel)
        {
            var mercatorMapSizeInLocalSpace = Math.Pow(2, zoomLevel - 1);
            var directionInMercator = new Vector2D(directionInLocalSpace.x, directionInLocalSpace.z) / mercatorMapSizeInLocalSpace;
            return directionInMercator;
        }

        /// <summary>
        /// Transforms an XYZ direction in the <see cref="MapRenderer"/>'s local space to a direction in Mercator space.
        /// </summary>
        public static Vector2D TransformWorldDirectionToMercator(this MapRenderer mapRenderer, Vector3 directionInWorldSpace)
        {
            return TransformWorldDirectionToMercator(mapRenderer, directionInWorldSpace, mapRenderer.ZoomLevel);
        }

        /// <summary>
        /// Transforms an XYZ direction in the <see cref="MapRenderer"/>'s local space to a direction in Mercator space.
        /// Uses the specified zoom level rather than the <see cref="MapRenderer"/>'s zoom level for the transformation.
        /// </summary>
        public static Vector2D TransformWorldDirectionToMercator(this MapRenderer mapRenderer, Vector3 directionInWorldSpace, double zoomLevel)
        {
            var directionInLocalSpace = mapRenderer.transform.InverseTransformDirection(directionInWorldSpace);
            directionInLocalSpace.x /= mapRenderer.transform.localScale.x;
            directionInLocalSpace.y /= mapRenderer.transform.localScale.y;
            directionInLocalSpace.z /= mapRenderer.transform.localScale.z;
            return TransformLocalDirectionToMercator(directionInLocalSpace, zoomLevel);
        }

        /// <summary>
        /// Transforms an XYZ point in world space to a Mercator position.
        /// </summary>
        public static Vector2D TransformWorldPointToMercator(this MapRenderer mapRenderer, Vector3 pointInWorldSpace)
        {
            var localSpacePoint = mapRenderer.transform.InverseTransformPoint(pointInWorldSpace);
            return TransformLocalPointToMercator(mapRenderer, localSpacePoint);
        }

        /// <summary>
        /// Transforms an XYZ point in world space to a Mercator position.
        /// </summary>
        public static Vector2D TransformWorldPointToMercatorWithAltitude(
            this MapRenderer mapRenderer,
            Vector3 pointInWorldSpace,
            out double altitudeInMeters,
            out double mercatorScale)
        {
            var localSpacePoint = mapRenderer.transform.InverseTransformPoint(pointInWorldSpace);
            return TransformLocalPointToMercatorWithAltitude(mapRenderer, localSpacePoint, out altitudeInMeters, out mercatorScale);
        }

        /// <summary>
        /// Transforms an XYZ point in world space to a <see cref="LatLon"/>.
        /// </summary>
        public static LatLon TransformWorldPointToLatLon(this MapRenderer mapRenderer, Vector3 pointInWorldSpace)
        {
            return new LatLon(mapRenderer.TransformWorldPointToMercator(pointInWorldSpace));
        }

        /// <summary>
        /// Transforms an XYZ point in world space to a <see cref="LatLonAlt"/>.
        /// </summary>
        public static LatLonAlt TransformWorldPointToLatLonAlt(this MapRenderer mapRenderer, Vector3 pointInWorldSpace)
        {
            var mercatorPosition =
                TransformLocalPointToMercatorWithAltitude(
                    mapRenderer,
                    pointInWorldSpace,
                    out var altitudeInMeters,
                    out var mercatorScale);

            var latLon = new LatLon(mercatorPosition);
            return new LatLonAlt(ref latLon, altitudeInMeters);
        }

        /// <summary>
        /// Transforms an XYZ point in world space to a <see cref="LatLonAlt"/>.
        /// </summary>
        public static Vector3 TransformLatLonAltToWorldPoint(this MapRenderer mapRenderer, LatLonAlt location)
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

            var pointInLocalSpace =
                new Vector3(
                    (float)localSpaceXZ.X,
                    (float)(altitudeInLocalSpace + mapRenderer.LocalMapHeight),
                    (float)localSpaceXZ.Y);

            return mapRenderer.transform.TransformPoint(pointInLocalSpace);
        }
    }
}
