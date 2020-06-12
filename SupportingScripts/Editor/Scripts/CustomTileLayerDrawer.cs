// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using System;
    using UnityEditor;

    /// <summary>
    /// Attribute indicating that a <see cref="PropertyDrawer"/> is intended to edit a class derived from <see cref="TextureTileLayer"/>.
    /// This class depends on <see cref="TileLayerDrawer"/> to instantiate and show it.
    /// </summary>
    public sealed class CustomTileLayerDrawer : Attribute
    {
        /// <summary>
        /// Gets the type that the drawer is used for.
        /// </summary>
        public Type TargetType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTileLayerDrawer"/> attribute.
        /// </summary>
        /// <param name="targetType">The type to use the drawer for.</param>
        public CustomTileLayerDrawer(Type targetType)
        {
            TargetType = targetType;
        }
    }
}
