// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using System;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(MapRenderer))]
    [CanEditMultipleObjects]
    internal class MapRendererEditor : Editor
    {
        private static bool _showLocationOptions = true;
        private SerializedProperty _centerProperty;
        private SerializedProperty _zoomLevelProperty;
        private SerializedProperty _minZoomLevelProperty;
        private SerializedProperty _maxZoomLevelProperty;
        private SerializedProperty _mapTerrainType;
        private SerializedProperty _mapShapeProperty;

        private static bool _showLayoutOptions = true;
        private SerializedProperty _localMapDimensionProperty;
        private SerializedProperty _localMapRadiusProperty;
        private SerializedProperty _localMapBaseHeightProperty;
        private SerializedProperty _mapColliderTypeProperty;

        private static bool _showRenderingOptions = true;
        private SerializedProperty _elevationScaleProperty;
        private SerializedProperty _castShadowsProperty;
        private SerializedProperty _receiveShadowsProperty;
        private SerializedProperty _enableMrtkMaterialIntegrationProperty;
        private SerializedProperty _terrainMaterialProperty;
        private SerializedProperty _isClippingVolumeWallEnabledProperty;
        private SerializedProperty _mapEdgeColorProperty;
        private SerializedProperty _mapEdgeColorFadeDistanceProperty;
        private SerializedProperty _clippingVolumeMaterialProperty;
        private SerializedProperty _clippingVolumeDistanceTextureResolutionProperty;
        private SerializedProperty _clippingDistanceLayerProperty;

        private static bool _showQualityOptions = true;
        private SerializedProperty _detailOffsetProperty;
        private SerializedProperty _numElevationTileFallbackLodsProperty;
        private SerializedProperty _requestTilesAroundViewProperty;

        private static bool _showTileLayerOptions = true;
        private SerializedProperty _textureTileLayersProperty;
        private SerializedProperty _elevationTileLayersProperty;
        private SerializedProperty _hideTileLayerComponentsProperty;

        private SerializedProperty _mapSessionProperty;

        private bool _useCustomTerrainMaterial;
        private bool _useCustomClippingVolumeMaterial;

        private GUIStyle _foldoutTitleStyle = null;
        private GUIStyle _boxStyle = null;
        private GUIStyle _subtextStyle = null;

        private readonly GUIContent[] _layerOptions =
            new GUIContent[]
            {
                new GUIContent("Default", "The map terrain consists of either elevation data or high resolution 3D models."),
                new GUIContent("Elevated", "The map terrain consists only of elevation data. No high resolution 3D models are used."),
                new GUIContent("Flat", "Both elevation and high resolution 3D models are disabled. The map will be flat.")
            };

        private readonly GUIContent[] _shapeOptions =
            new GUIContent[]
            {
                new GUIContent("Block", "Default shape. The map is rendered on a rectangular block."),
                new GUIContent("Cylinder", "Map is rendered on a cylinder."),
            };

        private readonly GUIContent[] _clippingVolumeDistanceTextureResolutionOptions =
            new GUIContent[]
            {
                new GUIContent("Low", "Low quality texture size. Uses less memory."),
                new GUIContent("Medium", "Medium quality texture size."),
                new GUIContent("High", "High quality texture size. Uses more memory.")
            };

        private readonly GUIContent[] _colliderOptions =
            new GUIContent[]
            {
                new GUIContent("No Collider", "No map collider."),
                new GUIContent("Base Only", "Collider covering the base of the map."),
                new GUIContent("Full Extents", "Collider covering the full extents of the map."),
            };

        private readonly GUILayoutOption[] _minMaxLabelsLayoutOptions =
            new GUILayoutOption[]
            {
                GUILayout.MaxWidth(52.0f)
            };

        private GUIContent _castShadowsLabel = new GUIContent("Cast Shadows");
        private GUIContent _receiveShadowsLabel = new GUIContent("Receive Shadows");
        private GUIContent _enableMrtkIntegrationLabel = new GUIContent("Enable MRTK Integration");
        private GUIContent _materialLabel = new GUIContent("Material");
        private GUIContent _renderClippingVolumeWallLabel = new GUIContent("Render Clipping Volume Wall");
        private GUIContent _colorLabel = new GUIContent("Color");
        private GUIContent _edgeFageLabel = new GUIContent("Edge Fade");
        private GUIContent _detailOffsetLabel = new GUIContent("Detail Offset");

        private readonly static int ControlIdHint = "MapRendererEditor".GetHashCode();

        private bool _isDragging = false;
        private Vector3 _startingHitPointInWorldSpace;
        private MercatorCoordinate _startingCenterInMercator;

        private void OnEnable()
        {
            _centerProperty = serializedObject.FindProperty("_center");
            _zoomLevelProperty = serializedObject.FindProperty("_zoomLevel");
            _minZoomLevelProperty = serializedObject.FindProperty("_minimumZoomLevel");
            _maxZoomLevelProperty = serializedObject.FindProperty("_maximumZoomLevel");
            _mapTerrainType = serializedObject.FindProperty("_mapTerrainType");
            _mapShapeProperty = serializedObject.FindProperty("_mapShape");
            _localMapDimensionProperty = serializedObject.FindProperty("_localMapDimension");
            _localMapRadiusProperty = serializedObject.FindProperty("_localMapRadius");
            _localMapBaseHeightProperty = serializedObject.FindProperty("_localMapBaseHeight");
            _elevationScaleProperty = serializedObject.FindProperty("_elevationScale");
            _castShadowsProperty = serializedObject.FindProperty("_castShadows");
            _receiveShadowsProperty = serializedObject.FindProperty("_receiveShadows");
            _enableMrtkMaterialIntegrationProperty = serializedObject.FindProperty("_enableMrtkMaterialIntegration");
            _terrainMaterialProperty = serializedObject.FindProperty("_terrainMaterial");
            _isClippingVolumeWallEnabledProperty = serializedObject.FindProperty("_isClippingVolumeWallEnabled");
            _clippingVolumeMaterialProperty = serializedObject.FindProperty("_clippingVolumeMaterial");
            _clippingVolumeDistanceTextureResolutionProperty = serializedObject.FindProperty("_clippingVolumeDistanceTextureResolution");
            _clippingDistanceLayerProperty = serializedObject.FindProperty("_clippingDistanceLayer");
            _mapEdgeColorProperty = serializedObject.FindProperty("_mapEdgeColor");
            _mapEdgeColorFadeDistanceProperty = serializedObject.FindProperty("_mapEdgeColorFadeDistance");
            _detailOffsetProperty = serializedObject.FindProperty("_detailOffset");
            _numElevationTileFallbackLodsProperty = serializedObject.FindProperty("_numElevationTileFallbackLods");
            _requestTilesAroundViewProperty = serializedObject.FindProperty("_requestTilesAroundView");
            _mapColliderTypeProperty = serializedObject.FindProperty("_mapColliderType");
            _textureTileLayersProperty = serializedObject.FindProperty("_textureTileLayers");
            _elevationTileLayersProperty = serializedObject.FindProperty("_elevationTileLayers");
            _hideTileLayerComponentsProperty = serializedObject.FindProperty("_hideTileLayerComponents");
            _mapSessionProperty = serializedObject.FindProperty("_mapSession");

            _useCustomTerrainMaterial = _terrainMaterialProperty.objectReferenceValue != null;
            _useCustomClippingVolumeMaterial = _clippingVolumeMaterialProperty.objectReferenceValue != null;

            EditorApplication.update += QueuePlayerLoopUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= QueuePlayerLoopUpdate;
        }

        private void OnSceneGUI()
        {
            var mapRenderer = target as MapRenderer;
            if (mapRenderer == null)
            {
                return;
            }

            if (Event.current.modifiers == EventModifiers.Control)
            {
                // Turn off the translation tool.
                Tools.hidden = true;

                // Change the cursor to make it obvious you can move the map around. Make the rect big enough to cover the scene view.
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, 999999, 999999), MouseCursor.MoveArrow);

                var currentEvent = Event.current;
                if (currentEvent.type == EventType.Layout)
                {
                    // Adding a control ID disables left-click-and-drag from creating a selection rect, rather than translating the map.
                    int controlID = GUIUtility.GetControlID(ControlIdHint, FocusType.Passive);
                    HandleUtility.AddDefaultControl(controlID);
                }
                else if (currentEvent.type == EventType.ScrollWheel)
                {
                    // Zoom map based on scroll wheel.
                    var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    if (mapRenderer.Raycast(ray, out var hitInfo))
                    {
                        Undo.RecordObject(mapRenderer, "Change ZoomLevel.");
                        var delta = -Event.current.delta;
                        mapRenderer.ZoomLevel += delta.y / 50;
                        currentEvent.Use();

                        EditorApplication.QueuePlayerLoopUpdate();
                    }
                }
                else if (currentEvent.type == EventType.MouseDown)
                {
                    // Begin panning if the mouse ray hits the map.
                    var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    if (mapRenderer.Raycast(ray, out var hitInfo))
                    {
                        if (Event.current.button == 1) // right-click
                        {
                            var menu = new GenericMenu();
                            menu.AddItem(
                                new GUIContent("Add MapPin"),
                                false,
                                (object o) =>
                                {
                                    var location = hitInfo.Location;
                                    var gameObject = new GameObject();
                                    gameObject.name = "MapPin";
                                    gameObject.transform.parent = mapRenderer.transform;
                                    var mapPin = gameObject.AddComponent<MapPin>();
                                    mapPin.Location = location.LatLon;
                                    mapPin.Altitude = location.AltitudeInMeters;
                                    mapPin.AltitudeReference = AltitudeReference.Ellipsoid;
                                    return;
                                },
                                0);
                            menu.ShowAsContext();
                            _isDragging = false;
                        }
                        else
                        {
                            _startingHitPointInWorldSpace = hitInfo.Point;
                            _startingCenterInMercator = mapRenderer.Center.ToMercatorCoordinate();
                            _isDragging = true;
                        }
                        currentEvent.Use();
                    }
                }
                else if (_isDragging && currentEvent.type == EventType.MouseDrag)
                {
                    // Update center 
                    var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    var plane = new Plane(mapRenderer.transform.up, _startingHitPointInWorldSpace);
                    if (plane.Raycast(ray, out var enter))
                    {
                        var updatedHitPointInWorldSpace = ray.GetPoint(enter);
                        var newDeltaInWorldSpace = updatedHitPointInWorldSpace - _startingHitPointInWorldSpace;
                        var newDeltaInLocalSpace = mapRenderer.transform.worldToLocalMatrix * newDeltaInWorldSpace;
                        var newDeltaInMercator = new MercatorCoordinate(newDeltaInLocalSpace.x, newDeltaInLocalSpace.z) / Math.Pow(2, mapRenderer.ZoomLevel - 1);
                        var newCenter = (_startingCenterInMercator - newDeltaInMercator).ToLatLon();

                        Undo.RecordObject(mapRenderer, "Change Center.");
                        mapRenderer.Center = newCenter;

                        EditorApplication.QueuePlayerLoopUpdate();
                    }

                    currentEvent.Use();
                }
                else if (_isDragging && currentEvent.type == EventType.MouseUp)
                {
                    _isDragging = false;

                    currentEvent.Use();
                }
            }
            else
            {
                Tools.hidden = false;
            }
        }

        public override void OnInspectorGUI()
        {
            Initialize();

            var mapRenderer = (MapRenderer)target;

            serializedObject.UpdateIfRequiredOrScript();

            EditorGUI.indentLevel++;
            // Location Section
            EditorGUILayout.BeginVertical(_boxStyle);
            _showLocationOptions = EditorGUILayout.Foldout(_showLocationOptions, "Location", true, _foldoutTitleStyle);
            if (_showLocationOptions)
            {
                var latitudeProperty = _centerProperty.FindPropertyRelative("_latitude");
                latitudeProperty.doubleValue = EditorGUILayout.DoubleField("Latitude", latitudeProperty.doubleValue);
                var longitudeProperty = _centerProperty.FindPropertyRelative("_longitude");
                longitudeProperty.doubleValue = EditorGUILayout.DoubleField("Longitude", longitudeProperty.doubleValue);

                EditorGUILayout.Slider(_zoomLevelProperty, MapConstants.MinimumZoomLevel, MapConstants.MaximumZoomLevel);
                // Get the zoomlevel values
                var minZoomLevel = _minZoomLevelProperty.floatValue;
                var maxZoomLevel = _maxZoomLevelProperty.floatValue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Zoom Level Range");
                EditorGUI.indentLevel--;
                minZoomLevel = EditorGUILayout.FloatField((float)Math.Round(minZoomLevel, 2), _minMaxLabelsLayoutOptions);
                EditorGUILayout.MinMaxSlider(ref minZoomLevel, ref maxZoomLevel, MapConstants.MinimumZoomLevel, MapConstants.MaximumZoomLevel);
                maxZoomLevel = EditorGUILayout.FloatField((float)Math.Round(maxZoomLevel, 2), _minMaxLabelsLayoutOptions);
                EditorGUI.indentLevel++;
                EditorGUILayout.EndHorizontal();

                // Update it back
                _minZoomLevelProperty.floatValue = minZoomLevel;
                _maxZoomLevelProperty.floatValue = maxZoomLevel;
                GUILayout.Space(4);
            }
            EditorGUILayout.EndVertical();

            // Map Layout Section
            EditorGUILayout.BeginVertical(_boxStyle);
            _showLayoutOptions = EditorGUILayout.Foldout(_showLayoutOptions, "Map Layout", true, _foldoutTitleStyle);
            if (_showLayoutOptions)
            {
                // Map Shape Controls
                GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Map Shape");
                _mapShapeProperty.enumValueIndex = GUILayout.Toolbar(_mapShapeProperty.enumValueIndex, _shapeOptions);
                GUILayout.EndHorizontal();

                GUILayout.Space(6f);

                if (_mapShapeProperty.enumValueIndex == (int)MapShape.Block)
                {
                    EditorGUILayout.PropertyField(_localMapDimensionProperty);
                    EditorGUILayout.LabelField(" ", "Scaled Map Dimension: " + mapRenderer.MapDimension.ToString());
                }
                else if (_mapShapeProperty.enumValueIndex == (int)MapShape.Cylinder)
                {
                    EditorGUILayout.PropertyField(_localMapRadiusProperty);
                    EditorGUILayout.LabelField(" ", "Scaled Map Radius: " + (mapRenderer.MapDimension.x / 2.0f).ToString());
                }
                GUILayout.Space(6f);
                EditorGUILayout.PropertyField(_localMapBaseHeightProperty);
                EditorGUILayout.LabelField(" ", "Scaled Map Base Height: " + mapRenderer.MapBaseHeight.ToString());
                GUILayout.Space(6f);

                // Map Collider Type Controls
                GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Map Collider Type");
                _mapColliderTypeProperty.enumValueIndex = GUILayout.Toolbar(_mapColliderTypeProperty.enumValueIndex, _colliderOptions);
                GUILayout.EndHorizontal();

                GUILayout.Space(6f);
            }
            EditorGUILayout.EndVertical();

            // Render Settings Section
            EditorGUILayout.BeginVertical(_boxStyle);
            _showRenderingOptions = EditorGUILayout.Foldout(_showRenderingOptions, "Render Settings", true, _foldoutTitleStyle);
            if (_showRenderingOptions)
            {
                // Map Terrain Type Controls
                GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Map Terrain Type");
                _mapTerrainType.enumValueIndex = GUILayout.Toolbar(_mapTerrainType.enumValueIndex, _layerOptions);
                GUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(_elevationScaleProperty);
                EditorGUILayout.PropertyField(_castShadowsProperty, _castShadowsLabel);
                EditorGUILayout.PropertyField(_receiveShadowsProperty, _receiveShadowsLabel);
                EditorGUILayout.PropertyField(_enableMrtkMaterialIntegrationProperty, _enableMrtkIntegrationLabel);

                {
                    var useCustomTerrainMaterial = EditorGUILayout.Toggle("Use Custom Terrain Material", _useCustomTerrainMaterial);
                    if (_useCustomTerrainMaterial && !useCustomTerrainMaterial)
                    {
                        // If the we were previously using a custom material, and have switched back to the default materials,
                        // reset the material to null which will cause the MapRenderer to reload the default material.
                        _terrainMaterialProperty.objectReferenceValue = null;
                    }

                    if (useCustomTerrainMaterial)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(_terrainMaterialProperty, _materialLabel);
                        EditorGUI.indentLevel--;
                    }

                    _useCustomTerrainMaterial = useCustomTerrainMaterial;
                }

                EditorGUILayout.PropertyField(_isClippingVolumeWallEnabledProperty, _renderClippingVolumeWallLabel);
                if (_isClippingVolumeWallEnabledProperty.boolValue)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(_mapEdgeColorProperty, _colorLabel);
                    _mapEdgeColorFadeDistanceProperty.floatValue =
                        EditorGUILayout.Slider(_edgeFageLabel, _mapEdgeColorFadeDistanceProperty.floatValue, 0, 1);

                    {
                        var useCustomClippingVolumeMaterial =
                            EditorGUILayout.Toggle("Use Custom Clipping Volume Material", _useCustomClippingVolumeMaterial);
                        if (_useCustomClippingVolumeMaterial && !useCustomClippingVolumeMaterial)
                        {
                            // If the we were previously using a custom material, and have switched back to the default materials,
                            // reset the material to null which will cause the MapRenderer to reload the default material.
                            _clippingVolumeMaterialProperty.objectReferenceValue = null;
                        }

                        if (useCustomClippingVolumeMaterial)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(_clippingVolumeMaterialProperty, _materialLabel);
                            EditorGUI.indentLevel--;
                        }

                        _useCustomClippingVolumeMaterial = useCustomClippingVolumeMaterial;
                    }

                    // Texture Camera Resolution
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Clipping Edge Resolution");
                    _clippingVolumeDistanceTextureResolutionProperty.enumValueIndex = GUILayout.Toolbar(
                        _clippingVolumeDistanceTextureResolutionProperty.enumValueIndex, _clippingVolumeDistanceTextureResolutionOptions);
                    GUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(_clippingDistanceLayerProperty);

                    EditorGUI.indentLevel--;
                }

                GUILayout.Space(6f);
            }
            EditorGUILayout.EndVertical();

            // Quality options.
            EditorGUILayout.BeginVertical(_boxStyle);
            _showQualityOptions = EditorGUILayout.Foldout(_showQualityOptions, "Quality", true, _foldoutTitleStyle);
            if (_showQualityOptions)
            {
                var position = EditorGUILayout.GetControlRect(false, 2 * EditorGUIUtility.singleLineHeight);
                position.height = EditorGUIUtility.singleLineHeight;

                position = EditorGUI.PrefixLabel(position, _detailOffsetLabel);
                EditorGUI.indentLevel--;

                _detailOffsetProperty.floatValue = EditorGUI.Slider(position, _detailOffsetProperty.floatValue, -1f, 1f);
                float labelWidth = position.width;

                // Render the sub-text labels.
                {
                    position.y += EditorGUIUtility.singleLineHeight;
                    position.width -= EditorGUIUtility.fieldWidth;

                    var color = GUI.color;
                    GUI.color = color * new Color(1f, 1f, 1f, 0.5f);

                    _subtextStyle.alignment = TextAnchor.UpperLeft;
                    EditorGUI.LabelField(position, "Low", _subtextStyle);

                    _subtextStyle.alignment = TextAnchor.UpperCenter;
                    EditorGUI.LabelField(position, "Default", _subtextStyle);

                    _subtextStyle.alignment = TextAnchor.UpperRight;
                    EditorGUI.LabelField(position, "High", _subtextStyle);

                    GUI.color = color;
                }

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_numElevationTileFallbackLodsProperty, new GUIContent("Elevation Tile Fallack LODs"));
                EditorGUILayout.PropertyField(_requestTilesAroundViewProperty);
            }
            EditorGUILayout.EndVertical();

            // Texture Tile Providers
            EditorGUILayout.BeginVertical(_boxStyle);
            _showTileLayerOptions = EditorGUILayout.Foldout(_showTileLayerOptions, "Tile Layers", true, _foldoutTitleStyle);
            if (_showTileLayerOptions)
            {
                EditorGUILayout.PropertyField(_textureTileLayersProperty, true);
                EditorGUILayout.PropertyField(_elevationTileLayersProperty, true);
                EditorGUILayout.PropertyField(_hideTileLayerComponentsProperty);
                GUILayout.Space(12f);
            }
            EditorGUILayout.EndVertical();


            GUILayout.Space(4);

            EditorGUILayout.PropertyField(_mapSessionProperty);

            serializedObject.ApplyModifiedProperties();
        }

        private void Initialize()
        {
            if (_foldoutTitleStyle == null)
            {
                _foldoutTitleStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = UnityEngine.FontStyle.Bold
                };
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
            }

            if (_subtextStyle == null)
            {
                _subtextStyle =
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.UpperLeft
                    };
            }
        }

        private void QueuePlayerLoopUpdate()
        {
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
