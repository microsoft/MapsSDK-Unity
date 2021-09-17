// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Maps.Unity.Services
{
    internal class Endpoints
    {
        public const string RestServiceDomain = "https://dev.virtualearth.net/REST/v1/";

        internal static string BuildUrl(string restApi, MapSession mapSession, string queryParameters)
        {
            var url =
                RestServiceDomain +
                $"{restApi}?" +
                (string.IsNullOrWhiteSpace(Uri.EscapeDataString(queryParameters)) ?
                    $"key={mapSession.DeveloperKey}" :
                    $"{queryParameters}&key={mapSession.DeveloperKey}");

            // Useful for debugging.
            //UnityEngine.Debug.Log(url);

            return url;
        }
    }
}
