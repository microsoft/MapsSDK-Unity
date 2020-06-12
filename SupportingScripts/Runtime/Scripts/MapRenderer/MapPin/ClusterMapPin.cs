// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.Maps.Unity
{
    using UnityEngine;

    /// <summary>
    /// Represents a cluster of <see cref="MapPin"/>s at the specified level of detail.
    /// </summary>
    [HelpURL("https://github.com/Microsoft/MapsSDK-Unity/wiki/Attaching-GameObjects-to-the-map")]
    public class ClusterMapPin : MapPin
    {
        /// <summary>
        /// The level of detail represented by this cluster.
        /// </summary>
        public short LevelOfDetail
        {
            get;
            internal set;
        }

        /// <summary>
        /// The number of pins in this cluster.
        /// </summary>
        public int Size
        {
            get;
            internal set;
        }
    }
}
