// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity.Search
{
    using Microsoft.Geospatial;

    /// <summary>
    /// Contains data about a specific location returned from a MapLocationFinder request.
    /// </summary>
    public class MapLocation
    {
        /// <summary>
        /// The name of the location, typically a formatted address.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The latitude and longitude coordinates of the location.
        /// </summary>
        public LatLon Point { get; }

        /// <summary>
        /// A structured address including common adress components like city, state, neighborhood, country/region, etc.
        /// </summary>
        public MapLocationAddress Address { get; }

        /// <summary>
        /// The classification of the location, such as 'Address' or 'Historical Site'.
        /// </summary>
        public string EntityType { get; }

        internal MapLocation(string displayName, LatLon point, MapLocationAddress address, string entityType)
        {
            DisplayName = displayName;
            Point = point;
            Address = address;
            EntityType = entityType;
        }
    }
}
