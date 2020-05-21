// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity.Search
{
    /// <summary>
    /// Encapsulates various options related to finding locations.
    /// </summary>
    public class MapLocationOptions
    {
        /// <summary>
        /// Specifies the maximum number of locations to return in the response.
        /// </summary>
        public int MaxResults { get; set; } = 5;

        /// <summary>
        /// Use the culture parameter to specify a culture for your request. The culture parameter provides the following strings in the
        /// language of the culture for geographic entities and place names returned by MapLocationFinder. For a list of supported culture
        /// codes, see https://docs.microsoft.com/en-us/bingmaps/rest-services/common-parameters-and-types/supported-culture-codes.
        /// </summary>
        public string Culture { get; set; } = null;

        /// <summary>
        /// A string that an ISO 3166-1 Alpha-2 region/country code. This will alter geopolitically disputed results to align with the specified region.
        /// https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2
        /// </summary>
        public string Region { get; set; } = null;

        /// <summary>
        /// Specifies to include the two-letter ISO country code with the address information in the response.
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
