// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Maps.Unity
{
    /// <summary>
    /// List of MapPins with callbacks for item addition and removal. Also, this list can be serialized.
    /// </summary>
    [Serializable]
    public class ObservableMapPinList : ObservableList<MapPin>
    {
    }
}
