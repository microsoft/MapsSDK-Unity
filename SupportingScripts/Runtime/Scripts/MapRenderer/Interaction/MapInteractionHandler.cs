// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEngine;

    /// <summary>
    /// The base class used for managing interactions with the <see cref="MapRenderer"/>. Implementions can handle a specific
    /// type of input, e.g. mouse-based input or touch-based input.
    /// </summary>
    [RequireComponent(typeof(MapInteractionController))]
    [RequireComponent(typeof(MapRenderer))]
    public abstract class MapInteractionHandler : MonoBehaviour
    {
        /// <summary>
        /// The duration in seconds after which a tap and hold event should be fired.
        /// </summary>
        public const float TapAndHoldThresholdInSeconds = 1.0f;

        /// <summary>
        /// The <see cref="Camera"/> associated with the interactions. Defaults to <see cref="Camera.main"/>.
        /// </summary>
        [SerializeField]
        private Camera _camera = null;

        /// <summary>
        /// The <see cref="Camera"/> associated with the interactions. Defaults to <see cref="Camera.main"/>.
        /// </summary>
        public Camera Camera => _camera;

        /// <summary>
        /// The associated <see cref="MapRenderer"/> that interactions are applied to.
        /// </summary>
        public MapRenderer MapRenderer { get; private set; }

        /// <summary>
        /// The <see cref="MapInteractionController"/> used to perform the operations for translating and zooming the map.
        /// </summary>
        public MapInteractionController MapInteractionController { get; private set; }

        /// <summary>
        /// The DPI scale used to normalize interaction magnitudes.
        /// </summary>
        public float DpiScale { get; private set; }

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        protected void Awake()
        {
            DpiScale = Mathf.Max(1.0f, Screen.dpi / 96.0f);

            MapRenderer = GetComponent<MapRenderer>();
            MapInteractionController = GetComponent<MapInteractionController>();

            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }
    }
}
