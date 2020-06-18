// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
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
        /// Transforms an XYZ point in the <see cref="MapRenderer"/>'s local space to a <see cref="MercatorCoordinate"/>.
        /// </summary>
        public static MercatorCoordinate TransformLocalPointToMercator(this MapRenderer mapRenderer, Vector3 pointInLocalSpace)
        {
            var deltaFromMapCenterToPointInMercator = TransformLocalDirectionToMercator(mapRenderer, pointInLocalSpace);
            return mapRenderer.Center.ToMercatorCoordinate() + deltaFromMapCenterToPointInMercator;
        }

        /// <summary>
        /// Transforms an XYZ point in the <see cref="MapRenderer"/>'s local space to a <see cref="MercatorCoordinate"/>.
        /// Includes the altitude measured as meters from the WGS84 ellipsoid.
        /// </summary>
        public static MercatorCoordinate TransformLocalPointToMercatorWithAltitude(
            this MapRenderer mapRenderer,
            Vector3 pointInLocalSpace,
            out double altitudeInMeters,
            out double mercatorScale)
        {
            var mercatorCoordinate = TransformLocalPointToMercator(mapRenderer, pointInLocalSpace);

            mercatorScale = MercatorScale.AtMercatorLatitude(mercatorCoordinate.Y);
            var equatorialCircumferenceInLocalSpace = Math.Pow(2, mapRenderer.ZoomLevel - 1);
            var elevationScale = (EquatorialCircumferenceInWgs84Meters / equatorialCircumferenceInLocalSpace) / mercatorScale / mapRenderer.ElevationScale;
            altitudeInMeters = (pointInLocalSpace.y - mapRenderer.LocalMapBaseHeight) * elevationScale + mapRenderer.ElevationBaseline;

            return mercatorCoordinate;
        }

        /// <summary>
        /// Transforms an XYZ direction in the <see cref="MapRenderer"/>'s local space to a direction in Mercator space.
        /// </summary>
        public static MercatorCoordinate TransformLocalDirectionToMercator(this MapRenderer mapRenderer, Vector3 directionInLocalSpace)
        {
            return TransformLocalDirectionToMercator(directionInLocalSpace, mapRenderer.ZoomLevel);
        }

        /// <summary>
        /// Transforms an XYZ direction in the <see cref="MapRenderer"/>'s local space to a direction in Mercator space.
        /// Uses the specified zoom level rather than the <see cref="MapRenderer"/>'s zoom level for the transformation.
        /// </summary>
        public static MercatorCoordinate TransformLocalDirectionToMercator(Vector3 directionInLocalSpace, double zoomLevel)
        {
            var mercatorMapSizeInLocalSpace = Math.Pow(2, zoomLevel - 1);
            var directionInMercator = new MercatorCoordinate(directionInLocalSpace.x, directionInLocalSpace.z) / mercatorMapSizeInLocalSpace;
            return directionInMercator;
        }

        /// <summary>
        /// Transforms an XYZ direction in the <see cref="MapRendererBase"/>'s local space to a direction in Mercator space.
        /// </summary>
        public static MercatorCoordinate TransformWorldDirectionToMercator(this MapRenderer mapRenderer, Vector3 directionInWorldSpace)
        {
            return TransformWorldDirectionToMercator(mapRenderer, directionInWorldSpace, mapRenderer.ZoomLevel);
        }

        /// <summary>
        /// Transforms an XYZ direction in the <see cref="MapRendererBase"/>'s local space to a direction in Mercator space.
        /// Uses the specified zoom level rather than the <see cref="MapRendererBase"/>'s zoom level for the transformation.
        /// </summary>
        public static MercatorCoordinate TransformWorldDirectionToMercator(this MapRenderer mapRenderer, Vector3 directionInWorldSpace, double zoomLevel)
        {
            var directionInLocalSpace = mapRenderer.transform.InverseTransformDirection(directionInWorldSpace);
            directionInLocalSpace.x /= mapRenderer.transform.localScale.x;
            directionInLocalSpace.y /= mapRenderer.transform.localScale.y;
            directionInLocalSpace.z /= mapRenderer.transform.localScale.z;
            return TransformLocalDirectionToMercator(directionInLocalSpace, zoomLevel);
        }

        /// <summary>
        /// Transforms an XYZ point in world space to a <see cref="MercatorCoordinate"/>.
        /// </summary>
        public static MercatorCoordinate TransformWorldPointToMercator(this MapRenderer mapRenderer, Vector3 pointInWorldSpace)
        {
            var pointInLocalSpace = mapRenderer.transform.InverseTransformPoint(pointInWorldSpace);
            return TransformLocalPointToMercator(mapRenderer, pointInLocalSpace);
        }

        /// <summary>
        /// Transforms an XYZ point in world space to a <see cref="MercatorCoordinate"/>.
        /// </summary>
        public static MercatorCoordinate TransformWorldPointToMercatorWithAltitude(
            this MapRenderer mapRenderer,
            Vector3 pointInWorldSpace,
            out double altitudeInMeters,
            out double mercatorScale)
        {
            var pointInLocalSpace = mapRenderer.transform.InverseTransformPoint(pointInWorldSpace);
            return TransformLocalPointToMercatorWithAltitude(mapRenderer, pointInLocalSpace, out altitudeInMeters, out mercatorScale);
        }

        /// <summary>
        /// Transforms an XYZ point in world space to a <see cref="LatLon"/>.
        /// </summary>
        public static LatLon TransformWorldPointToLatLon(this MapRenderer mapRenderer, Vector3 pointInWorldSpace)
        {
            return mapRenderer.TransformWorldPointToMercator(pointInWorldSpace).ToLatLon();
        }

        /// <summary>
        /// Transforms an XYZ point in world space to a <see cref="LatLonAlt"/>.
        /// </summary>
        public static LatLonAlt TransformWorldPointToLatLonAlt(this MapRenderer mapRenderer, Vector3 pointInWorldSpace)
        {
            var pointInLocalSpace = mapRenderer.transform.InverseTransformPoint(pointInWorldSpace);
            var mercatorCoordinate =
                TransformLocalPointToMercatorWithAltitude(
                    mapRenderer,
                    pointInLocalSpace,
                    out var altitudeInMeters,
                    out var mercatorScale);

            var latLon = mercatorCoordinate.ToLatLon();
            return new LatLonAlt(ref latLon, altitudeInMeters);
        }

        /// <summary>
        /// Transforms a <see cref="LatLonAlt"/> to an XYZ point in world space.
        /// </summary>
        public static Vector3 TransformLatLonAltToWorldPoint(this MapRenderer mapRenderer, LatLonAlt location)
        {
            var pointInLocalSpace = mapRenderer.TransformLatLonAltToLocalPoint(location);
            return mapRenderer.transform.TransformPoint(pointInLocalSpace);
        }

        /// <summary>
        /// Transforms a <see cref="LatLonAlt"/> to an XYZ point in local space.
        /// </summary>
        public static Vector3 TransformLatLonAltToLocalPoint(this MapRenderer mapRenderer, LatLonAlt location)
        {
            // Can compute XZ coords in local space from the Mercator coordinate of  thie location and the map center.
            var mercatorCoordinate = location.LatLon.ToMercatorCoordinate();
            return mapRenderer.TransformMercatorWithAltitudeToLocalPoint(mercatorCoordinate, location.AltitudeInMeters);
        }

        /// <summary>
        /// Transforms a <see cref="MercatorCoordinate"/> to an XYZ point in local space.
        /// </summary>
        public static Vector3 TransformMercatorWithAltitudeToLocalPoint(
            this MapRenderer mapRenderer,
            in MercatorCoordinate mercatorCoordinate,
            double altitudeInMeters)
        {
            var mercatorCoordinateRelativeToCenter = mercatorCoordinate - mapRenderer.Center.ToMercatorCoordinate();
            var equatorialCircumferenceInLocalSpace = Math.Pow(2, mapRenderer.ZoomLevel - 1);
            var localSpaceXZ = equatorialCircumferenceInLocalSpace * mercatorCoordinateRelativeToCenter;

            // Altitude to local y value.
            var offsetMapAltitudeInMeters = altitudeInMeters - mapRenderer.ElevationBaseline;
            var mercatorScale = MercatorScale.AtMercatorLatitude(mercatorCoordinate.Y);
            var altitudeInLocalSpace =
                mapRenderer.ElevationScale *
                offsetMapAltitudeInMeters *
                mercatorScale *
                (equatorialCircumferenceInLocalSpace / EquatorialCircumferenceInWgs84Meters);
            altitudeInLocalSpace += mapRenderer.LocalMapBaseHeight;

            return new Vector3((float)localSpaceXZ.X, (float)altitudeInLocalSpace, (float)localSpaceXZ.Y);
        }

        /// <summary>
        /// Transforms a <see cref="MercatorCoordinate"/> to an XYZ point in world space.
        /// </summary>
        public static Vector3 TransformMercatorWithAltitudeToWorldPoint(
            this MapRenderer mapRenderer,
            in MercatorCoordinate mercatorCoordinate,
            double altitudeInMeters)
        {
            var pointInLocalSpace = mapRenderer.TransformMercatorWithAltitudeToLocalPoint(mercatorCoordinate, altitudeInMeters);
            return mapRenderer.transform.TransformPoint(pointInLocalSpace);
        }
    }
}
