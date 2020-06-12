// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.Maps.Unity
{
    using System;

    /// <summary>
    /// List of MapPins with callbacks for item addition and removal. Also, this list can be serialized.
    /// </summary>
    [Serializable]
    public class ObservableMapPinList : ObservableList<MapPin>
    {
    }
}
