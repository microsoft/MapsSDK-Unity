// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity.Search
{
    using System;

    [Serializable]
    internal class RestResponse
    {
        public string copyright = null;

        public ResourceSet[] resourceSets = null;

        public int statusCode = 0;

        public string statusDescription = null;
    }
}
