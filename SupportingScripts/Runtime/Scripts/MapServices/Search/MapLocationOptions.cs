// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity.Search
{
    /// <summary>
    /// Encapsulates various options related to finding locations.
    /// </summary>
    public class MapLocationOptions : ServiceOptions
    {
        /// <summary>
        /// Specifies the maximum number of locations to return in the response.
        /// </summary>
        public int MaxResults { get; set; } = 5;

        /// <summary>
        /// Specifies to include the two-letter ISO country/region code with the address information in the response.
        /// By default, MapLocationAddress will not include this.
        /// </summary>
        public bool IncludeCountryCode { get; set; }

        /// <summary>
        /// Specifies to include the neighborhood with the address information in the response, if available.
        /// By default, MapLocationAddress will not include this.
        /// </summary>
        public bool IncludeNeighborhood { get; set; }
    }
}
