// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity.Search
{
    using Microsoft.Geospatial;
    using Microsoft.Maps.Unity.Services;
    using System;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Networking;

    /// <summary>
    /// Handles geocoding and reverse geocoding requests. Based on the input query string, which can represent an address or landmark,
    /// or the input location represented by a latitude and longitude, the MapLocationFinder will provide richer address information.
    /// <para/>
    /// This API can be used for standard geocoding and reverse geocoding scenarios. It does not currently provide location information
    /// from queries using generic business types, e.g. "Coffee", "Pizza", etc.
    /// </summary>
    public static partial class MapLocationFinder
    {
        /// <summary>
        /// Forms and sends a geocoding request to retrieve location infromation for the provided address query.
        /// </summary>
        public async static Task<MapLocationFinderResult> FindLocations(string query, MapLocationOptions mapLocationOptions = null)
        {
            var mapLocationOptionsForRequest = mapLocationOptions ?? new MapLocationOptions();
            if (!ValidateMapSession(mapLocationOptionsForRequest))
            {
                return new MapLocationFinderResult(MapLocationFinderStatus.InvalidCredentials);
            }

            var url = await BuildUrl($"q={query}", mapLocationOptions ?? new MapLocationOptions()).ConfigureAwait(true);
            return await Request(url).ConfigureAwait(true);
        }

        /// <summary>
        /// Forms and sends a geocoding request to retrieve location infromation for the provided address query.
        /// The specified reference location helps improve the result's relevance.
        /// </summary>
        public async static Task<MapLocationFinderResult> FindLocations(
            string query,
            LatLon referenceLocation,
            MapLocationOptions mapLocationOptions = null)
        {
            var mapLocationOptionsForRequest = mapLocationOptions ?? new MapLocationOptions();
            if (!ValidateMapSession(mapLocationOptionsForRequest))
            {
                return new MapLocationFinderResult(MapLocationFinderStatus.InvalidCredentials);
            }

            var url = await BuildUrl($"q={query}", mapLocationOptions ?? new MapLocationOptions()).ConfigureAwait(true);
            url += $"&ul={ referenceLocation.LatitudeInDegrees},{ referenceLocation.LongitudeInDegrees}";
            return await Request(url).ConfigureAwait(true);
        }

        /// <summary>
        /// Forms and sends a geocoding request to retrieve location infromation for the provided address query.
        /// The specified GeoBoundingBox helps improve the result's relevance.
        /// </summary>
        public async static Task<MapLocationFinderResult> FindLocations(
            string query,
            GeoBoundingBox referenceBoundingBox,
            MapLocationOptions mapLocationOptions = null)
        {
            var mapLocationOptionsForRequest = mapLocationOptions ?? new MapLocationOptions();
            if (!ValidateMapSession(mapLocationOptionsForRequest))
            {
                return new MapLocationFinderResult(MapLocationFinderStatus.InvalidCredentials);
            }

            var url = await BuildUrl($"q={query}", mapLocationOptionsForRequest).ConfigureAwait(true);
            url +=
                $"&umv={referenceBoundingBox.BottomLeft.LatitudeInDegrees},{referenceBoundingBox.BottomLeft.LongitudeInDegrees},{referenceBoundingBox.TopRight.LatitudeInDegrees},{referenceBoundingBox.TopRight.LongitudeInDegrees}";
            return await Request(url).ConfigureAwait(true);
        }

        /// <summary>
        /// Forms and sends a reverse geocoding request by location.
        /// </summary>
        public static async Task<MapLocationFinderResult> FindLocationsAt(LatLon location, MapLocationOptions mapLocationOptions = null)
        {
            var mapLocationOptionsForRequest = mapLocationOptions ?? new MapLocationOptions();
            if (!ValidateMapSession(mapLocationOptionsForRequest))
            {
                return new MapLocationFinderResult(MapLocationFinderStatus.InvalidCredentials);
            }

            var url =
                await BuildUrl(
                    string.Empty,
                    mapLocationOptions ?? new MapLocationOptions(),
                    $"Locations/{location.LatitudeInDegrees},{location.LongitudeInDegrees}",
                    false)
                .ConfigureAwait(true);

            return await Request(url).ConfigureAwait(true);
        }

        private async static Task<string> BuildUrl(
            string baseQuery,
            MapLocationOptions mapLocationOptions,
            string resource = "Locations",
            bool startWithAmpersand = true)
        {
            var culture = await mapLocationOptions.GetCulture().ConfigureAwait(true);
            var region = await mapLocationOptions.GetRegion().ConfigureAwait(true);

            if (string.IsNullOrWhiteSpace(region))
            {
                Debug.LogWarning("No region has been specified for the MapLocationFinder request.");
            }

            var parameters =
                $"{baseQuery}" +
                (startWithAmpersand ? "&" : "") +
                $"maxRes={Math.Max(mapLocationOptions.MaxResults, 1)}" +
                (string.IsNullOrWhiteSpace(culture) ? "" : $"&c={culture}") +
                (string.IsNullOrWhiteSpace(region) ? "" : $"&ur={region}") +
                (mapLocationOptions.IncludeCountryCode ? "&incl=ciso2" : "") +
                (mapLocationOptions.IncludeNeighborhood ? "&inclnb=1" : "") +
                "&sftr=teolin";

            return Endpoints.BuildUrl(resource, mapLocationOptions.MapSession, parameters);
        }

        private static bool ValidateMapSession(MapLocationOptions mapLocationOptions)
        {
            if (mapLocationOptions.MapSession == null)
            {
                Debug.LogError("MapLocationFinder request failed: MapSession is not present.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(mapLocationOptions.MapSession.DeveloperKey))
            {
                Debug.LogError("MapLocationFinder request failed: MapSession has missing developer key.");
                return false;
            }

            return true;
        }

        private static async Task<MapLocationFinderResult> Request(string url)
        {
            var webRequest = UnityWebRequest.Get(new Uri(url));
            await webRequest.SendWebRequest();

            // Check error codes and parse the data. Note, we're on the Unity main thread here.

            if (webRequest.isNetworkError)
            {
                return new MapLocationFinderResult(MapLocationFinderStatus.NetworkFailure);
            }
            else if (webRequest.isHttpError)
            {
                if (webRequest.responseCode == 401)
                {
                    return new MapLocationFinderResult(MapLocationFinderStatus.InvalidCredentials);
                }
                else if (webRequest.responseCode >= 500)
                {
                    return new MapLocationFinderResult(MapLocationFinderStatus.ServerError);
                }
                else
                {
                    return new MapLocationFinderResult(MapLocationFinderStatus.UnknownError);
                }
            }
            else
            {
                // Consider moving parsing work to a background thread.
                var jsonString = webRequest.downloadHandler.text;
                var restResponse = JsonUtility.FromJson<RestResponse>(jsonString);
                return new MapLocationFinderResult(restResponse);
            }
        }
    }
}
