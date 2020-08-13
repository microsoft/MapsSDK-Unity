// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using System.Threading.Tasks;

    /// <summary>
    /// Options common to all REST-based Bing Maps services.
    /// </summary>
    public class ServiceOptions
    {
        private MapSession _mapSession;

        /// <summary>
        /// The <see cref="Unity.MapSession"/> to associate with this request. By default, uses <see cref="MapSession.Current"/>.
        /// The <see cref="MapSession"/> provides credentials for requests as well as localization values for language and region.
        /// </summary>
        public MapSession MapSession
        {
            get => _mapSession ?? MapSession.Current;
            set => _mapSession = value;
        }

        /// <summary>
        /// Use this property to specify a culture for the request. This value takes overrides the the language and region that would
        /// otherwise be automatically used from <see cref="MapSession.Current"/>.
        /// <br/><br/>
        /// The culture value provides the strings in the language of the culture for geographic entities and place names returned
        /// by MapLocationFinder. For a list of supported culture codes, see
        /// https://docs.microsoft.com/en-us/bingmaps/rest-services/common-parameters-and-types/supported-culture-codes.
        /// </summary>
        public string CultureOverride { get; set; }

        /// <summary>
        /// Use this property to specify a specific region for the request. This value takes precedent over teh region taht would otherwise
        /// be automatically used from <see cref="MapSession.Current"/>.
        /// <br/><br/>
        /// A region value is an ISO 3166-1 Alpha-2 country/region code. This will alter geopolitically disputed results to align with the
        /// specified region. https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2
        /// </summary>
        public string RegionOverride { get; set; }

        /// <summary>
        /// A string representing the language and region, e.g. "en-US".
        /// <br/><br/>
        /// The culture value provides the strings in the language of the culture for geographic entities and place names returned
        /// by MapLocationFinder. For a list of supported culture codes, see
        /// https://docs.microsoft.com/en-us/bingmaps/rest-services/common-parameters-and-types/supported-culture-codes.
        /// </summary>
        public async Task<string> GetCulture()
        {
            if (!string.IsNullOrWhiteSpace(CultureOverride))
            {
                return CultureOverride;
            }

            if (MapSession != null)
            {
                var language = MapSession.Language;
                var region = await GetRegion().ConfigureAwait(true);
                if (!string.IsNullOrWhiteSpace(region))
                {
                    return SystemLangaugeConverter.ToCultureCode(language, region);
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// A string representing the region, e.g. "en-US".
        /// <br/><br/>
        /// A region value is an ISO 3166-1 Alpha-2 country/region code. This will alter geopolitically disputed results to align with the
        /// specified region. https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2
        /// </summary>
        public async Task<string> GetRegion()
        {
            if (!string.IsNullOrWhiteSpace(RegionOverride))
            {
                return RegionOverride;
            }

            if (MapSession != null)
            {
                return await MapSession.GetRegion().ConfigureAwait(true);
            }

            return string.Empty;
        }
    }
}
