// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity.Search
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains status code and result location data for a <see cref="MapLocationFinder"/> request.
    /// </summary>
    public class MapLocationFinderResult
    {
        /// <summary>
        /// Location data returned in the result.
        /// </summary>
        /// <remarks>
        /// If <see cref="Status"/> is not set to <see cref="MapLocationFinderStatus.Success"/>, the list will be empty.
        /// </remarks>
        public IReadOnlyList<MapLocation> Locations { get; }

        /// <summary>
        /// The status of the result.
        /// </summary>
        public MapLocationFinderStatus Status { get; } 

        internal MapLocationFinderResult(MapLocationFinderStatus status)
        {
            Locations = new List<MapLocation>();
            Status = status;
        }

        internal MapLocationFinderResult(RestResponse restResponse)
        {
            var locations = new List<MapLocation>();
            Locations = locations;

            try
            {
                foreach (var resourceSet in restResponse.resourceSets)
                {
                    foreach (var resource in resourceSet.resources)
                    {
                        var mapLocation = new MapLocation(resource.name, resource.point, resource.address, resource.entityType);
                        locations.Add(mapLocation);
                    }
                }
            }
            catch (Exception)
            {
                Status = MapLocationFinderStatus.BadResponse;
                return; // Early out.
            }

            Status = locations.Count > 0 ? MapLocationFinderStatus.Success : MapLocationFinderStatus.EmptyResponse;
        }
    }
}
