// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEditor;

    /// <summary>
    /// Encapsulates a <see cref="PropertyDrawer"/> that can be used to display
    /// editor UI for a property of type <see cref="TextureTileLayerList"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(TextureTileLayerList))]
    public class TextureTileLayerListDrawer : TileLayerListDrawer<TextureTileLayer>
    {
        /// <inheritdoc />
        protected override int MaximumCount => 4;

        /// <inheritdoc />
        protected override string Title => "Texture Tile Layers";
    }
}
