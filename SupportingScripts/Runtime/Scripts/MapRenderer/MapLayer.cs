// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEngine;

    /// <summary>
    /// Base class for any type of layer associated with a <see cref="Unity.MapRenderer"/>.
    /// </summary>
    [RequireComponent(typeof(MapRenderer))]
    public abstract class MapLayer : MonoBehaviour
    {
        /// <summary>
        /// The name of the <see cref="MapLayer"/>.
        /// </summary>
        [SerializeField]
        private string _layerName;

        /// <summary>
        /// The name of the <see cref="MapLayer"/>.
        /// </summary>
        public string LayerName { get => _layerName; set => _layerName = value; }

        private MapRenderer _mapRenderer;

        /// <summary>
        /// The <see cref="MapRenderer"/> that this layer has been attached to.
        /// </summary>
        public MapRenderer MapRenderer
        {
            get
            {
                if (_mapRenderer == null)
                {
                    _mapRenderer = GetComponent<MapRenderer>();

                    if (_mapRenderer == null)
                    {
                        Debug.LogError($"Unable to find MapRenderer component on '{gameObject.name}'.");
                        return null;
                    }
                }

                return _mapRenderer;
            }
        }
    }
}
