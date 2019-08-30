// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity.Search
{
    using Microsoft.Geospatial;
    using System;

    [Serializable]
    internal class Point
    {
        public string type = "";

        public double[] coordinates = null;
        
        public static implicit operator LatLon(Point point) => new LatLon(point.coordinates[0], point.coordinates[1]);
    }
}
