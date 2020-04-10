// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEditor;

    /// <summary>
    /// Encapsulates a <see cref="PropertyDrawer"/> that can be used to display
    /// editor UI for a property of type <see cref="ElevationTileLayerList"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(ElevationTileLayerList))]
    public class ElevationTileLayerListDrawer : TileLayerListDrawer<ElevationTileLayer>
    {
        /// <inheritdoc />
        protected override string Title => "Elevation Tile Layers";
    }
}
