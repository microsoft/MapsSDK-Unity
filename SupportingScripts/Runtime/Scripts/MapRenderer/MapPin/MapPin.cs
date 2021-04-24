// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// A MapPin can be used to pin a <see cref="GameObject"/> to a <see cref="MapRendererBase"/> at a specified
    /// <see cref="LatLon"/> and altitude in meters.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    [HelpURL("https://github.com/Microsoft/MapsSDK-Unity/wiki/Attaching-GameObjects-to-the-map")]
    public class MapPin : MonoBehaviour, IPinnable
    {
        [SerializeField]
        private LatLonWrapper _location;

        /// <summary>
        /// The location of the <see cref="MapPin"/>.
        /// </summary>
        public LatLon Location
        {
            get
            {
                return _location.ToLatLon();
            }
            set
            {
                var oldLocation = _location.ToLatLon();
                if (value != oldLocation)
                {
                    _location = new LatLonWrapper(value);
                    MercatorCoordinate = _location.ToLatLon().ToMercatorCoordinate();
                    LocationChanged?.Invoke(this, oldLocation); // Invoke action with the MapPin and old location.
                }
            }
        }

        /// <summary>
        /// <see cref="Action"/> that is invoked when the <see cref="Location"/> value has changed.
        /// The <see cref="LatLon"/> specified in the arguments is the previous location.
        /// </summary>
        public Action<MapPin, LatLon> LocationChanged { get; set; }

        /// <inheritdoc/>
        public MercatorCoordinate MercatorCoordinate { get; private set; }

        [SerializeField]
        private double _altitude;

        /// <inheritdoc/>
        public double Altitude { get => _altitude; set => _altitude = value; }

        [SerializeField]
        private AltitudeReference _altitudeReference = AltitudeReference.Surface;

        /// <inheritdoc/>
        public AltitudeReference AltitudeReference
        {
            get => _altitudeReference;
            set => _altitudeReference = value;
        }

        /// <inheritdoc/>
        public Vector3 PositionInMapLocalSpace
        {
            get => transform.localPosition;
            set => transform.localPosition = value;
        }

        /// <inheritdoc/>
        public bool HasBeenFullyPositioned { get; set; }

        /// <summary>
        /// If true, synchronizes this <see cref="GameObject"/>'s and it's childrens' layers to the same value as the
        /// associated <see cref="MapRendererBase"/>'s layer.
        /// </summary>
        [SerializeField]
        [FormerlySerializedAs("IsLayerSynchronized")]
        private bool _isLayerSynchronized = true;

        /// <summary>
        /// If true, synchronizes this <see cref="GameObject"/>'s and it's childrens' layers to the same value as the
        /// associated <see cref="MapRendererBase"/>'s layer.
        /// </summary>
        public bool IsLayerSynchronized
        {
            get => _isLayerSynchronized;
            set => _isLayerSynchronized = value;
        }

        /// <summary>
        /// If true, the <see cref="ScaleCurve"/> is relative to the real-world scale at a given zoom level. As the map zooms out,
        /// size falls off exponentially. If false, the ScaleCurve represents the direct scale of the MapPin at a given zoom level.
        /// </summary>
        [SerializeField]
        [FormerlySerializedAs("UseRealWorldScale")]
        private bool _useRealWorldScale;

        /// <summary>
        /// If true, the <see cref="ScaleCurve"/> is relative to the real-world scale at a given zoom level. As the map zooms out,
        /// size falls off exponentially. If false, the ScaleCurve represents the direct scale of the MapPin at a given zoom level.
        /// </summary>
        public bool UseRealWorldScale
        {
            get => _useRealWorldScale;
            set => _useRealWorldScale = value;
        }

        /// <summary>
        /// The scale of the pin relative to the map's zoom level.
        /// </summary>
        [SerializeField]
        [FormerlySerializedAs("ScaleCurve")]
        private AnimationCurve _scaleCurve = AnimationCurve.Linear(MapConstants.MinimumZoomLevel, 1, MapConstants.MaximumZoomLevel, 1);

        /// <summary>
        /// The scale of the pin relative to the map's zoom level.
        /// </summary>
        public AnimationCurve ScaleCurve
        {
            get => _scaleCurve;
            set => _scaleCurve = value;
        }

        /// <summary>
        /// If true, when the <see cref="MapPin"/> is outside of the <see cref="MapRenderer"/>'s bounds, it will be hidden.
        /// Otherwise, it will always be shown. Default is to hide when outside of map's bounds.
        /// </summary>
        [SerializeField]
        private bool _showOutsideMapBounds = false;

        /// <summary>
        /// If true, when the <see cref="MapPin"/> is outside of the <see cref="MapRenderer"/>'s bounds, it will be shown.
        /// Otherwise, it will be hidden. Default is to hide when outside of map's bounds.
        /// </summary>
        public bool ShowOutsideMapBounds
        {
            get => _showOutsideMapBounds;
            set => _showOutsideMapBounds = value;
        }

        /// <summary>
        /// Reset the component to default values.
        /// </summary>
        protected virtual void Reset()
        {
            MercatorCoordinate = Location.ToMercatorCoordinate();
        }

        /// <summary>
        /// Awake is called when the component instance is being loaded.
        /// </summary>
        protected virtual void Awake()
        {
            MercatorCoordinate = Location.ToMercatorCoordinate();
        }

        /// <summary>
        /// OnEnable is called when the component is enabled and when scripts are reloaded.
        /// </summary>
        protected virtual void OnEnable()
        {
            MercatorCoordinate = Location.ToMercatorCoordinate();
        }

        /// <summary>
        /// This method is called when the script is loaded or a value is changed in the inspector. Called in the editor only.
        /// </summary>
        protected virtual void OnValidate()
        {
            MercatorCoordinate = Location.ToMercatorCoordinate();
        }

        /// <summary>
        /// Synchronizes the <see cref="MapPin"/>'s layers (and any of it's children layers) with the <see cref="MapRenderer"/>'s layer.
        /// Whether or not a <see cref="MapPin"/>'s layer is synchronized depends on the value of <see cref="IsLayerSynchronized"/>.
        /// </summary>
        public static void SynchronizeLayers(IReadOnlyList<MapPin> mapPins, MapRenderer mapRenderer)
        {
            if (mapPins == null)
            {
                throw new ArgumentNullException(nameof(mapPins));
            }

            if (mapRenderer == null)
            {
                throw new ArgumentNullException(nameof(mapRenderer));
            }

            var targetLayer = mapRenderer.gameObject.layer;
            foreach (var mapPin in mapPins)
            {
                if (mapPin.IsLayerSynchronized && mapPin.gameObject.layer != targetLayer)
                {
                    mapPin.gameObject.layer = targetLayer;
                    foreach (var child in mapPin.GetComponentsInChildren<Transform>())
                    {
                        child.gameObject.layer = targetLayer;
                    }
                }
            }
        }

        /// <summary>
        ///  Updates the <see cref="MapPin"/>s' scale based on <see cref="ScaleCurve"/> and <see cref="UseRealWorldScale"/>.
        /// </summary>
        public static void UpdateScales<T>(List<T> mapPins, MapRenderer mapRenderer) where T : MapPin
        {
            if (mapPins == null)
            {
                throw new ArgumentNullException(nameof(mapPins));
            }

            if (mapRenderer == null)
            {
                throw new ArgumentNullException(nameof(mapRenderer));
            }

            const double EquatorialCircumferenceInWgs84Meters = 40075016.685578488;
            var mapZoomLevel = mapRenderer.ZoomLevel;
            var mapTotalWidthInLocalSpace = Math.Pow(2, Math.Max(mapZoomLevel - 1.0, 0.0));
            var mapRealisticScale = mapTotalWidthInLocalSpace / EquatorialCircumferenceInWgs84Meters;
            var mapElevationScale = mapRenderer.ElevationScale;

            foreach (var mapPin in mapPins)
            {
                if (mapPin.enabled) // Skip MapPins that aren't enabled.
                {
                    // Scale the pin depending on the scale curve and current ZoomLevel.
                    var scale = mapPin.ScaleCurve.Evaluate(mapZoomLevel);

                    var additionalYScale = 1.0f;
                    if (mapPin.UseRealWorldScale)
                    {
                        var mercatorScaleAtCoordinate = MercatorScale.AtMercatorLatitude(mapPin.MercatorCoordinate.Y);
                        scale *= (float)(mercatorScaleAtCoordinate * mapRealisticScale);
                        additionalYScale = mapElevationScale;
                    }

                    mapPin.transform.localScale = new Vector3(scale, scale * additionalYScale, scale);
                }
            }
        }
    }
}
