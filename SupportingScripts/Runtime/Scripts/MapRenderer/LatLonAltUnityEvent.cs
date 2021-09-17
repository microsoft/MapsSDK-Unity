// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Geospatial;
using System;
using UnityEngine.Events;

namespace Microsoft.Maps.Unity
{
    /// <summary>
    /// An event that provides a <see cref="LatLonAlt"/>.
    /// </summary>
    [Serializable]
    public class LatLonAltUnityEvent : UnityEvent<LatLonAlt> { }
}
