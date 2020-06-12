// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity.Search
{
    using System;

    [Serializable]
    internal class Address
    {
        public string addressLine = null;

        public string adminDistrict = null;

        public string adminDistrict2 = null;

        public string countryRegion = null;

        public string formattedAddress = null;

        public string locality = null;

        public string neighborhood = null;

        public string landmark = null;

        public string postalCode = null;

        public string countryRegionIso2 = null;

        public static implicit operator MapLocationAddress(Address address) => new MapLocationAddress(address);
    }
}
