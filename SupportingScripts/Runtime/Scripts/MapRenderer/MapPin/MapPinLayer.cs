// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.Maps.Unity
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Profiling;

    /// <summary>
    /// Maintains a collection of <see cref="MapPin"/>s to efficient display large number of objects on the map. Supports clustering.
    /// <br/><br/>
    /// All instantiated <see cref="MapPin"/>s associated with this layer will be parented to a child <see cref="GameObject"/> with the
    /// same name as this <see cref="MapPinLayer"/>.
    /// <br/><br/>
    /// <see cref="MapPin.ShowOutsideMapBounds"/> is not used by the layer. Any out of view pin will be hidden.
    /// </summary>
    [HelpURL("https://github.com/Microsoft/MapsSDK-Unity/wiki/Attaching-GameObjects-to-the-map")]
    public class MapPinLayer : MapLayer
    {
        private MapPinSpatialIndex _mapPinSpatialIndex;

        [SerializeField]
        private ObservableMapPinList _mapPins = new ObservableMapPinList();

        /// <summary>
        /// All MapPins associated with this MapPinLayer.
        /// </summary>
        public ObservableList<MapPin> MapPins { get { return _mapPins; } }

        /// <summary>
        /// The MapPins which are active.
        /// </summary>
        public IReadOnlyCollection<MapPin> ActiveMapPins => _activeMapPins;

        /// <summary>
        /// The ClusterMapPins which are active.
        /// </summary>
        public IReadOnlyCollection<ClusterMapPin> ActiveClusterMapPins => _activeClusterMapPins;

        /// <summary>
        /// True if the MapPins in this data source should be clustered. Note, if this is set to true, it is expected that a prefab
        /// has been provided to ClusterMapPinPrefab.
        /// </summary>
        [SerializeField]
        private bool _isClusteringEnabled = false;

        /// <summary>
        /// True if the MapPins in this data source should be clustered. Note, if this is set to true, it is expected that a prefab
        /// has been provided to ClusterMapPinPrefab.
        /// </summary>
        public bool IsClusteringEnabled
        {
            get => _isClusteringEnabled;
            set
            {
                if (_isClusteringEnabled != value)
                {
                    _isClusteringEnabled = value;
                    RebuildSpatialIndex();
                }
            }
        }

        /// <summary>
        /// If the number of pins in a spatial region exceed the ClusterThreshold, a single cluster MapPin will be rendered instead.
        /// </summary>
        [SerializeField]
        private int _clusterThreshold = 5;

        /// <summary>
        /// If the number of pins in a spatial region exceed the ClusterThreshold, a single cluster MapPin will be rendered instead.
        /// </summary>
        /// <remarks>
        /// Modifying during ruintime will cause the MapPinLayer to rebuild, which may be expensive.
        /// </remarks>
        public int ClusterThreshold
        {
            get => _clusterThreshold;
            set
            {
                value = Math.Max(2, value);
                if (_clusterThreshold != value)
                {
                    _clusterThreshold = value;
                    RebuildSpatialIndex();
                }
            }
        }

        /// <summary>
        /// The prefab to use for clusters.
        /// </summary>
        [SerializeField]
        private ClusterMapPin _clusterMapPinPrefab;

        /// <summary>
        /// The prefab to use for clusters.
        /// </summary>
        /// <remarks>
        /// Modifying during ruintime will cause the MapPinLayer to rebuild, which may be expensive.
        /// </remarks>
        public ClusterMapPin ClusterMapPinPrefab
        {
            get => _clusterMapPinPrefab;
            set
            {
                if (value != _clusterMapPinPrefab)
                {
                    _clusterMapPinPrefab = value;
                    RebuildSpatialIndex();
                }
            }
        }

        private GameObject _containerGo;
        private readonly HashSet<MapPin> _mapPinsInViewThisFrame = new HashSet<MapPin>();
        private readonly HashSet<MapPin> _activeMapPins = new HashSet<MapPin>();
        private readonly HashSet<ClusterMapPin> _clusterMapPinsInViewThisFrame = new HashSet<ClusterMapPin>();
        private readonly HashSet<ClusterMapPin> _activeClusterMapPins = new HashSet<ClusterMapPin>();

        private void OnValidate()
        {
            _clusterThreshold = Math.Max(_clusterThreshold, 2);
        }

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(LayerName))
            {
                LayerName = "MapPinLayer";
            }
        }

        private void OnEnable()
        {
            // Hook up ObservableList events.
            MapPins.ItemAdded += OnItemAdded;
            MapPins.RangeAdded += OnRangeAdded;
            MapPins.ItemRemoved += OnItemRemoved;
            MapPins.RangeRemoved += OnRangeRemoved;

            EnsureContainerGameObjectIsCreated();
            _containerGo.gameObject.SetActive(true);

            EnsureSpatialIndexInitialized();

            MapRenderer.AfterUpdate -= UpdateMapPinsInView;
            MapRenderer.AfterUpdate += UpdateMapPinsInView;
            MapRenderer.AfterOnDisable -= MapRendererDisabled;
            MapRenderer.AfterOnDisable += MapRendererDisabled;
        }

        private void OnDisable()
        {
            _mapPinSpatialIndex.DestroyClusterMapPins();
            _activeClusterMapPins.Clear();

            foreach (var mapPin in _activeMapPins)
            {
                mapPin.gameObject.SetActive(false);
            }
            _activeMapPins.Clear();

            // Unregister events.
            MapRenderer.AfterUpdate -= UpdateMapPinsInView;
            MapRenderer.AfterOnDisable -= MapRendererDisabled;

            EnsureContainerGameObjectIsCreated();
            _containerGo.gameObject.SetActive(false);

            // Unhook ObservableList events.
            MapPins.ItemAdded -= OnItemAdded;
            MapPins.RangeAdded -= OnRangeAdded;
            MapPins.ItemRemoved -= OnItemRemoved;
            MapPins.RangeRemoved -= OnRangeRemoved;
        }

        private void OnDestroy()
        {
            MapPins.Clear();

            Destroy(_containerGo);
            _containerGo = null;
        }

        private void UpdateMapPinsInView(object sender, EventArgs args)
        {
            Profiler.BeginSample("UpdateMapPinsInView");

            if (_mapPinSpatialIndex != null)
            {
                EnsureContainerGameObjectIsCreated();
                _containerGo.gameObject.SetActive(true);

                List<MapPin> mapPinsInView;
                List<ClusterMapPin> clusterMapPinsInView;
                if (MapRenderer.MapShape == MapShape.Block)
                {
                    _mapPinSpatialIndex.GetPinsInView(
                        MapRenderer.MercatorBoundingBox,
                        MapRenderer.ZoomLevel,
                        _clusterMapPinPrefab,
                        _containerGo.transform,
                        out mapPinsInView,
                        out clusterMapPinsInView);
                }
                else // Cylinder.
                {
                    _mapPinSpatialIndex.GetPinsInView(
                        MapRenderer.MercatorBoundingBox,
                        MapRenderer.MercatorBoundingCircle,
                        MapRenderer.ZoomLevel,
                        _clusterMapPinPrefab,
                        _containerGo.transform,
                        out mapPinsInView,
                        out clusterMapPinsInView);
                }

                // Update visible MapPins' position and other properties.
                {
                    MapPin.SynchronizeLayers(mapPinsInView, MapRenderer);
                    MapPin.SynchronizeLayers(clusterMapPinsInView, MapRenderer);
                    MapRenderer.TrackAndPositionPinnables(mapPinsInView);
                    MapRenderer.TrackAndPositionPinnables(clusterMapPinsInView);
                    MapPin.UpdateScales(mapPinsInView, MapRenderer);
                    MapPin.UpdateScales(clusterMapPinsInView, MapRenderer);
                }

                // Disable MapPins that are no longer visible.
                {
                    _mapPinsInViewThisFrame.Clear();
                    _mapPinsInViewThisFrame.UnionWith(mapPinsInView);

                    foreach (var previousActiveMapPin in _activeMapPins)
                    {
                        if (previousActiveMapPin != null && !_mapPinsInViewThisFrame.Contains(previousActiveMapPin))
                        {
                            MapRenderer.UntrackPinnable(previousActiveMapPin);
                            previousActiveMapPin.gameObject.SetActive(false);
                        }
                    }

                    _clusterMapPinsInViewThisFrame.Clear();
                    _clusterMapPinsInViewThisFrame.UnionWith(clusterMapPinsInView);

                    foreach (var previousActiveClusterMapPin in _activeClusterMapPins)
                    {
                        if (previousActiveClusterMapPin != null && !_clusterMapPinsInViewThisFrame.Contains(previousActiveClusterMapPin))
                        {
                            MapRenderer.UntrackPinnable(previousActiveClusterMapPin);
                            previousActiveClusterMapPin.gameObject.SetActive(false);
                        }
                    }

                    // Filter out pins that have not been fully positioned, i.e. are still awaiting an elevation sample.
                    // As a side effect, this will also enable any pins once any initial async op used to position them has completed.

                    _mapPinsInViewThisFrame.RemoveWhere(
                        (MapPin mapPin) =>
                        {
                            mapPin.gameObject.SetActive(mapPin.HasBeenFullyPositioned);
                            return !mapPin.HasBeenFullyPositioned;
                        });

                    _clusterMapPinsInViewThisFrame.RemoveWhere(
                        (ClusterMapPin clusterMapPin) =>
                        {
                            clusterMapPin.gameObject.SetActive(clusterMapPin.HasBeenFullyPositioned);
                            return !clusterMapPin.HasBeenFullyPositioned;
                        });

                    // Assign collections to layer properties.
                    {
                        _activeMapPins.Clear();
                        _activeMapPins.UnionWith(_mapPinsInViewThisFrame);
                        _mapPinsInViewThisFrame.Clear();

                        _activeClusterMapPins.Clear();
                        _activeClusterMapPins.UnionWith(_clusterMapPinsInViewThisFrame);
                        _clusterMapPinsInViewThisFrame.Clear();
                    }
                }
            }

            Profiler.EndSample();
        }

        private void MapRendererDisabled(object sender, EventArgs args)
        {
            EnsureContainerGameObjectIsCreated();
            _containerGo.gameObject.SetActive(false);
        }

        private void OnItemAdded(object sender, MapPin mapPin, int index)
        {
            _mapPinSpatialIndex.AddMapPin(mapPin);
            mapPin.gameObject.SetActive(false);

            EnsureContainerGameObjectIsCreated();
            mapPin.transform.SetParent(_containerGo.transform, false);
        }

        private void OnRangeAdded(object sender, IEnumerable<MapPin> mapPins, int index)
        {
            foreach (var mapPin in mapPins)
            {
                _mapPinSpatialIndex.AddMapPin(mapPin);
            }

            foreach (var mapPin in mapPins)
            {
                mapPin.gameObject.SetActive(false);
            }

            EnsureContainerGameObjectIsCreated();
            foreach (var mapPin in mapPins)
            {
                mapPin.transform.SetParent(_containerGo.transform, false);
            }
        }

        private void OnItemRemoved(object sender, MapPin mapPin, int index)
        {
            _mapPinSpatialIndex.RemoveMapPin(mapPin);
        }

        private void OnRangeRemoved(object sender, IEnumerable<MapPin> mapPins, int index)
        {
            foreach (var mapPin in mapPins)
            {
                _mapPinSpatialIndex.RemoveMapPin(mapPin);
            }
        }

        private void EnsureContainerGameObjectIsCreated()
        {
            if (_containerGo == null)
            {
                // Create a GO to hold the pins if one doesn't already exist.
                var mapPinContainerName =
                    string.IsNullOrWhiteSpace(LayerName) ? "Unnamed MapPinLayer Container" : LayerName + " Container";
                _containerGo = new GameObject(mapPinContainerName);
                _containerGo.transform.SetParent(transform, false);
            }
        }

        private void EnsureSpatialIndexInitialized()
        {
            if (_mapPinSpatialIndex != null)
            {
                return;
            }

            _mapPinSpatialIndex = new MapPinSpatialIndex(_isClusteringEnabled, _clusterThreshold);

            // If there was already been MapPins added to the data source, add them to the spatial index as well.
            {
                foreach (var mapPin in MapPins)
                {
                    _mapPinSpatialIndex.AddMapPin(mapPin);
                }

                foreach (var mapPin in MapPins)
                {
                    mapPin.gameObject.SetActive(false);
                }

                foreach (var mapPin in MapPins)
                {
                    mapPin.transform.SetParent(_containerGo.transform, false);
                }
            }
        }

        private void RebuildSpatialIndex()
        {
            if (_mapPinSpatialIndex != null)
            {
                _mapPinSpatialIndex.DestroyClusterMapPins();
                _mapPinSpatialIndex = null;
            }

            EnsureSpatialIndexInitialized();
        }
    }
}
