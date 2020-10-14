// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using System;
    using UnityEngine.Events;

    /// <summary>
    /// An event that provides a <see cref="LatLonAlt"/>.
    /// </summary>
    [Serializable]
    public class LatLonAltUnityEvent : UnityEvent<LatLonAlt> { }
}
