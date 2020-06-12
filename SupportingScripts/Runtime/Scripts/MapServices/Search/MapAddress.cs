// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity.Search
{
    /// <summary>
    /// Contains address data for a specific location returned from a geocoding request.
    /// </summary>
    public class MapLocationAddress
    {
        /// <summary>
        /// Specifies the address in a local format. This address may not include the country/region, e.g.
        /// "1 Microsoft Way, Redmond, WA 98052-8300"
        /// </summary>
        public string FormattedAddress { get; }

        /// <summary>
        /// A string specifying the populated place for the address.
        /// This typically refers to a city, but may refer to a suburb or a neighborhood depending on the country/region.
        /// </summary>
        public string AddressLine { get; }

        /// <summary>
        /// A string specifying the neighborhood for an address.
        /// </summary>
        public string Neighborhood { get; }

        /// <summary>
        /// A string specifying the populated place for the address.
        /// This typically refers to a city, but may refer to a suburb or a neighborhood depending on the country/region.
        /// </summary>
        public string Locality { get; }

        /// <summary>
        /// A string specifying the subdivision name in the country/region for an address.
        /// This element is typically treated as the first order administrative subdivision,
        /// but in some cases it is the second, third, or fourth order subdivision in a country/region.
        /// </summary>
        public string AdminDistrict { get; }

        /// <summary>
        /// A string specifying the subdivision name in the country/region for an address.
        /// This element is used when there is another level of subdivision information for a location, such as the county.
        /// </summary>
        public string AdminDistrict2 { get; }

        /// <summary>
        /// A string specifying the post code, postal code, or ZIP Code of an address.
        /// </summary>
        public string PostalCode { get; }

        /// <summary>
        /// A string specifying the country/region name of an address.
        /// </summary>
        public string CountryRegion { get; }

        /// <summary>
        /// A string specifying the two-letter ISO country/region code.
        /// </summary>
        public string CountryCode { get; }

        /// <summary>
        /// A string specifying the name of the landmark when there is a landmark associated with an address.
        /// </summary>
        public string Landmark { get; }

        internal MapLocationAddress(Address address)
        {
            FormattedAddress = address.formattedAddress ?? "";
            AddressLine = address.addressLine ?? "";
            Neighborhood = address.neighborhood ?? "";
            Locality = address.locality ?? "";
            AdminDistrict = address.adminDistrict ?? "";
            AdminDistrict2 = address.adminDistrict2 ?? "";
            PostalCode = address.postalCode ?? "";
            CountryRegion = address.countryRegion ?? "";
            CountryCode = address.countryRegionIso2 ?? "";
            Landmark = address.landmark ?? "";
        }
    }
}
