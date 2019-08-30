// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity.Services
{
    using System;

    internal class Endpoints
    {
        public const string RestServiceDomain = "https://dev.virtualearth.net/REST/v1/";

        internal static string BuildUrl(string restApi, string queryParameters)
        {
            var url =
                RestServiceDomain +
                $"{restApi}?" +
                (string.IsNullOrWhiteSpace(Uri.EscapeDataString(queryParameters)) ?
                    $"key={MapServices.BingMapsKey ?? ""}" :
                    $"{queryParameters}&key={MapServices.BingMapsKey ?? ""}");

            // Useful for debugging.
            //UnityEngine.Debug.Log(url);

            return url;
        }
    }
}
