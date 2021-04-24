// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Profiling;

    /// <summary>
    /// Manages streaming and rendering of map data.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/Microsoft/MapsSDK-Unity/wiki/Configuring-the-MapRenderer")]
    public sealed class MapRenderer : MapRendererBase
    {
        /// <summary>
        /// The type of collider that the map is using.
        /// </summary>
        [SerializeField]
        private MapColliderType _mapColliderType = MapColliderType.BaseOnly;

        /// <summary>
        /// The type of collider that the map is using.
        /// </summary>
        public MapColliderType MapColliderType { get => _mapColliderType; set => _mapColliderType = value; }
        private MapColliderType _previousMapColliderType = MapColliderType.BaseOnly;

        /// <summary>
        /// The <see cref="Collider"/> used for the map. The dimensions are synchronized to match the map's layout.
        /// Null value if no <see cref="Collider"/> is active.
        /// </summary>
        public Collider MapCollider => _mapCollider;

        /// <summary>
        /// The <see cref="Collider"/> used for the map. The dimensions are synchronized to match the map's layout.
        /// Null value if no <see cref="Collider"/> is active.
        /// </summary>
        [SerializeField]
        private Collider _mapCollider = null;

        /// <summary>
        /// The collider used for all map shape types although in the future may just apply to box mode.
        /// </summary>
        private BoxCollider _mapBoxCollider;

        private IMapSceneAnimationController _activeMapSceneAnimationController;

        // Manages MapPins attached directly as children to the MapRenderer. Behaves like a MapPinLayer, but no clustering or indexing.
        private bool _checkChildMapPins = true;
        private readonly HashSet<MapPin> _mapPinChildrenSet = new HashSet<MapPin>();
        private readonly List<MapPin> _lastMapPinsInView = new List<MapPin>();
        private readonly List<MapPin> _mapPinsInView = new List<MapPin>();
        private readonly HashSet<MapPin> _currentChildrenMapPins = new HashSet<MapPin>();
        private readonly List<MapPin> _mapPinChildrenToRemove = new List<MapPin>();

        /// <summary>
        /// Called after the <see cref="MapRenderer"/> has executed Update().
        /// </summary>
        /// <remarks>
        /// Use this callback when performing operations to a <see cref="MapLayer"/> that depend on various map properties.
        /// At this point in the lifecycle, <see cref="MapRendererBase.Center"/>, <see cref="MapRendererBase.ZoomLevel"/>,
        /// and other properties related to the position of the map will reflect the values used for this frame,
        /// i.e. it is after any animations have ran and the properties used to position and render the map content in this frame
        /// have already been determined.
        /// </remarks>
        public event EventHandler AfterUpdate;

        /// <summary>
        /// Returns a yieldable object that can be used to wait until the map has completed loading.
        /// </summary>
        public WaitForMapLoaded WaitForLoad(float maxWaitDurationInSeconds = 30.0f)
        {
            return new WaitForMapLoaded(this, maxWaitDurationInSeconds);
        }

        /// <summary>
        /// Sets the <see cref="MapRenderer"/>'s view to reflect the new <see cref="MapScene"/>.
        /// </summary>
        /// <returns>
        /// A yieldable object is returned that can be used to wait for the end of the animation in a coroutine.
        /// </returns>
        public WaitForMapSceneAnimation SetMapScene(
            MapScene mapScene,
            MapSceneAnimationKind mapSceneAnimationKind = MapSceneAnimationKind.Bow,
            float animationTimeScale = 1.0f)
        {
            return
                SetMapScene(
                    mapScene,
                    new MapSceneAnimationController(),
                    mapSceneAnimationKind,
                    animationTimeScale);
        }

        /// <summary>
        /// Sets the <see cref="MapRenderer"/>'s view to reflect the new <see cref="MapScene"/>
        /// using the specified <see cref="IMapSceneAnimationController"/>.
        /// </summary>
        /// <returns>
        /// A yieldable object is returned that can be used to wait for the end of the animation in a coroutine.
        /// </returns>
        public WaitForMapSceneAnimation SetMapScene(
            MapScene mapScene,
            IMapSceneAnimationController mapSceneAnimationController,
            MapSceneAnimationKind mapSceneAnimationKind = MapSceneAnimationKind.Bow,
            float animationTimeScale = 1.0f)
        {
            if (mapScene == null)
            {
                throw new ArgumentNullException(nameof(mapScene));
            }

            if (mapSceneAnimationController == null)
            {
                throw new ArgumentNullException(nameof(mapSceneAnimationController));
            }

            // If we were in the middle of a previous animation, make sure it has yielded and then reset it.
            CancelAnimation();

            mapScene.GetLocationAndZoomLevel(out var finalCenter, out var finalZoomLevel);

            animationTimeScale = Mathf.Max(0, animationTimeScale);

            if (mapSceneAnimationKind == MapSceneAnimationKind.None || animationTimeScale == 0.0)
            {
                // Snap the view.
                ZoomLevel = (float)finalZoomLevel;
                Center = finalCenter;
            }
            else
            {
                try
                {
                    // Otherwise, setup an animation.
                    mapSceneAnimationController.Initialize(this, mapScene, animationTimeScale, mapSceneAnimationKind);
                    _activeMapSceneAnimationController = mapSceneAnimationController;
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to initialize the IMapSceneAnimationController.\r\n" + e, gameObject);
                    _activeMapSceneAnimationController = null;
                }
            }

            // Return a yield instruction from the animation controller itself (if we have one). Otherwise, we were setting a scene
            // without animation, so just return a completed yield instruction.
            return
                _activeMapSceneAnimationController == null ?
                    new WaitForMapSceneAnimation(true /* isComplete */) :
                    _activeMapSceneAnimationController.YieldInstruction;
        }

        /// <inheritdoc/>
        protected override bool RunAnimation(out LatLon newCenter, out float newZoomLevel)
        {
            if (_activeMapSceneAnimationController != null)
            {
                try
                {
                    if (_activeMapSceneAnimationController.UpdateAnimation(ZoomLevel, Center, out newZoomLevel, out newCenter))
                    {
                        // Animation is complete.
                        _activeMapSceneAnimationController = null;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to update animation.\r\n" + e, gameObject);
                    _activeMapSceneAnimationController = null;
                }
            }

            newCenter = new LatLon();
            newZoomLevel = 0;
            return false;
        }

        /// <inheritdoc/>
        protected override void CancelAnimation()
        {
            if (_activeMapSceneAnimationController == null)
            {
               return;
            }

            _activeMapSceneAnimationController.YieldInstruction.SetComplete();
            _activeMapSceneAnimationController = null;
        }

        /// <inheritdoc/>
        protected override void OneTimeSetup(int lastVersion)
        {
            // Runs one-time setup to add sibling components which are present by default but can be removed later.
            // The logic here also takes into account the "version" of  the Maps SDK as some setup should only
            // be ran when upgrading from one version to a newer version. See MapRenderer.Version docs for mapping of
            // each version integer to Maps SDK version.

            if (lastVersion < 1)
            {
                // Add a MapCopyrightLayer by default.
                var existingMapCopyrightLayer = GetComponent<MapCopyrightLayer>();
                if (existingMapCopyrightLayer == null)
                {
                    gameObject.AddComponent<MapCopyrightLayer>();
                }

                // Initialize default layers.
                if (TextureTileLayers.Count == 0)
                {
                    gameObject.AddComponent<DefaultTextureTileLayer>();
                }
            }

            if (lastVersion < 2)
            {
                if (ElevationTileLayers.Count == 0)
                {
                    // Adds the DefaultElevationLayer to the now Serialized list of elevation layers.
                    gameObject.AddComponent<DefaultElevationTileLayer>();
                }
            }

            if (lastVersion < 3)
            {
                // Grab a reference to the first box collider that is found (if any).
                var existingColliders = GetComponents<BoxCollider>();
                if (existingColliders != null)
                {
                    foreach (var collider in existingColliders)
                    {
                        _mapCollider = collider;
                        _mapBoxCollider = collider;
                        break;
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();

            _checkChildMapPins = true;

            // Create a MapDataCache if it doesn't exist.
            if (FindObjectOfType<MapDataCacheBase>() == null)
            {
                gameObject.AddComponent<MapDataCache>();
            }
        }

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();

            _checkChildMapPins = true;

            // Create a MapDataCache if it doesn't exist.
            if (FindObjectOfType<MapDataCacheBase>() == null)
            {
                gameObject.AddComponent<MapDataCache>();
            }

            if (_mapCollider != null)
            {
                _mapCollider.enabled = true;
            }
        }

        private void OnTransformChildrenChanged()
        {
            _checkChildMapPins = true;
        }

        /// <inheritdoc/>
        protected override void Update()
        {
            base.Update();

            UpdateChildrenMapPins();

            UpdateMapCollider();

            {
                Profiler.BeginSample("AfterUpdate");
                AfterUpdate?.Invoke(this, EventArgs.Empty);
                Profiler.EndSample();
            }
        }

        /// <inheritdoc/>
        protected override void OnDisable()
        {
            base.OnDisable();

            foreach (var childMapPin in _lastMapPinsInView)
            {
                childMapPin.gameObject.SetActive(false);
            }

            if (_mapCollider != null)
            {
                _mapCollider.enabled = false;
            }
        }

        private void UpdateMapCollider()
        {
            if (_mapColliderType != MapColliderType.None)
            {
                if (_mapCollider == null)
                {
                    _mapBoxCollider = gameObject.AddComponent<BoxCollider>();
                    _mapCollider = _mapBoxCollider;
                }
                else if (_mapBoxCollider == null)
                {
                    // The box collider field is just used to prevent recasting _mapCollider every frame.
                    // Since it's not serialized it may be null even if _mapCollider isn't.
                    _mapBoxCollider = _mapCollider as BoxCollider;
                }

                _mapCollider.enabled = true;

                var colliderHeight = (_mapColliderType == MapColliderType.BaseOnly) ? LocalMapBaseHeight : LocalMapHeight;
                _mapBoxCollider.center = new Vector3(0, colliderHeight / 2, 0);
                if (MapShape == MapShape.Block)
                {
                    _mapBoxCollider.size = new Vector3(LocalMapDimension.x, colliderHeight, LocalMapDimension.y);
                }
                else
                {
                    _mapBoxCollider.size = new Vector3(LocalMapRadius * 2.0f, colliderHeight, LocalMapRadius * 2.0f);
                }
            }
            else
            {
                if (_previousMapColliderType != MapColliderType.None && _mapCollider != null)
                {
                    DestroyImmediate(_mapCollider);
                    _mapCollider = null;
                    _mapBoxCollider = null;
                }
            }

            _previousMapColliderType = _mapColliderType;
        }

        private void UpdateChildrenMapPins()
        {
            Profiler.BeginSample("UpdateChildrenMapPins");

            _mapPinsInView.Clear();

            // Get all the direct descendant MapPins of this GameObject that are in view.
            {
                if (_checkChildMapPins)
                {
                    _currentChildrenMapPins.Clear();

                    var currentChildrenMapPins = GetComponentsInChildren<MapPin>(true);
                    foreach (var currentChildMapPin in currentChildrenMapPins)
                    {
                        if (currentChildMapPin.transform.parent == transform)
                        {
                            _currentChildrenMapPins.Add(currentChildMapPin);
                        }
                    }

                    // Add any new MapPin children.
                    foreach (var mapPin in _currentChildrenMapPins)
                    {
                        if (_mapPinChildrenSet.Add(mapPin))
                        {
                            // This is a new MapPin. Deactivate until it's position has been calculated.
                            mapPin.gameObject.SetActive(false);
                        }
                    }

                    // Remove any MapPins that are no longer children or have been disabled.
                    {
                        _mapPinChildrenToRemove.Clear();
                        foreach (var existingChildMapPin in _mapPinChildrenSet)
                        {
                            if (!_currentChildrenMapPins.Contains(existingChildMapPin))
                            {
                                _mapPinChildrenToRemove.Add(existingChildMapPin);
                            }
                        }

                        foreach (var mapPinChildToRemove in _mapPinChildrenToRemove)
                        {
                            _mapPinChildrenSet.Remove(mapPinChildToRemove);
                        }
                    }

                    _currentChildrenMapPins.Clear();
                    _mapPinChildrenToRemove.Clear();
                    _checkChildMapPins = false;
                }

                // Get the MapPins in view.
                if (_mapPinChildrenSet.Count > 0)
                {
                    if (MapShape == MapShape.Block)
                    {
                        var mercatorBoundingBox = MercatorBoundingBox;
                        foreach (var mapPin in _mapPinChildrenSet)
                        {
                            if (mapPin.ShowOutsideMapBounds || mercatorBoundingBox.Intersects(mapPin.Location))
                            {
                                _mapPinsInView.Add(mapPin);
                            }
                        }
                    }
                    else // Cylinder
                    {
                        var mercatorBoundingCircle = MercatorBoundingCircle;
                        foreach (var mapPin in _mapPinChildrenSet)
                        {
                            if (mapPin.ShowOutsideMapBounds || mercatorBoundingCircle.Intersects(mapPin.MercatorCoordinate))
                            {
                                _mapPinsInView.Add(mapPin);
                            }
                        }
                    }
                }
            }

            TrackAndPositionPinnables(_mapPinsInView);

            // Disable any MapPins that were in the last view but is not the current view.
            {
                foreach (var lastMapPinInView in _lastMapPinsInView)
                {
                    if (lastMapPinInView != null && !_mapPinsInView.Contains(lastMapPinInView))
                    {
                        UntrackPinnable(lastMapPinInView);
                        lastMapPinInView.gameObject.SetActive(false);
                    }
                }
                _lastMapPinsInView.Clear();
                _lastMapPinsInView.AddRange(_mapPinsInView);
            }

            MapPin.SynchronizeLayers(_mapPinsInView, this);
            MapPin.UpdateScales(_mapPinsInView, this);

            // Ensure the MapPins that have had their positions computed have been enabled.
            foreach (var mapPinInView in _mapPinsInView)
            {
                mapPinInView.gameObject.SetActive(mapPinInView.HasBeenFullyPositioned && mapPinInView.enabled);
            }

            Profiler.EndSample();
        }
    }
}
