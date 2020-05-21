// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using Microsoft.Geospatial.VectorMath;
    using System;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(MapRenderer))]
    [CanEditMultipleObjects]
    internal class MapRendererEditor : Editor
    {
        private SerializedProperty _bingMapsKeyProperty;
        private SerializedProperty _showMapDataInEditorProperty;
        private static bool _showMapLocationOptions = true;
        private SerializedProperty _centerProperty;
        private SerializedProperty _zoomLevelProperty;
        private SerializedProperty _minZoomLevelProperty;
        private SerializedProperty _maxZoomLevelProperty;
        private SerializedProperty _mapTerrainType;
        private SerializedProperty _mapShapeProperty;
        private static bool _showMapSizingOptions = true;
        private SerializedProperty _mapEdgeColorProperty;
        private SerializedProperty _mapEdgeColorFadeDistanceProperty;
        private SerializedProperty _localMapDimensionProperty;
        private SerializedProperty _localMapRadiusProperty;
        private SerializedProperty _localMapHeightProperty;
        private static bool _terrainOptions = true;
        private SerializedProperty _elevationScaleProperty;
        private SerializedProperty _castShadowsProperty;
        private SerializedProperty _receiveShadowsProperty;
        private SerializedProperty _enableMrtkMaterialIntegrationProperty;
        private SerializedProperty _useCustomTerrainMaterialProperty;
        private SerializedProperty _customTerrainMaterialProperty;
        private SerializedProperty _isClippingVolumeWallEnabledProperty;
        private SerializedProperty _useCustomClippingVolumeMaterialProperty;
        private SerializedProperty _customClippingVolumeMaterialProperty;
        private SerializedProperty _clippingVolumeDistanceTextureResolution;
        private SerializedProperty _labelPrefabProperty;
        private static bool _showQualityOptions = true;
        private SerializedProperty _detailOffsetProperty;
        private SerializedProperty _maxCacheSizeInBytesProperty;
        private SerializedProperty _mapColliderTypeProperty;
        private static bool _showTileLayers = true;
        private SerializedProperty _textureTileLayersProperty;
        private SerializedProperty _elevationTileLayersProperty;
        private SerializedProperty _hideTileLayerComponentsProperty;
        private static bool _showLocalizationOptions = true;
        private SerializedProperty _languageOverrideProperty;
        private GUIStyle _baseStyle = null;
        private GUIStyle _hyperlinkStyle = null;
        private GUIStyle _errorIconStyle = null;
        private GUIContent _errorIcon = null;
        private GUIStyle _foldoutTitleStyle = null;
        private GUIStyle _boxStyle = null;
        private Texture2D _bannerWhite = null;
        private Texture2D _bannerBlack = null;
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
        
        private readonly Tuple<UnityEngine.Object, UnityEngine.Object>[] _terrainMaterials =
            new Tuple<UnityEngine.Object, UnityEngine.Object>[3];

        private static int ControlIdHint = "MapRendererEditor".GetHashCode();

        private bool _isDragging = false;
        private Vector3 _startingHitPointInWorldSpace;
        private Vector2D _startingCenterInMercatorSpace;

        private void OnEnable()
        {
            _centerProperty = serializedObject.FindProperty("_center");
            _bingMapsKeyProperty = serializedObject.FindProperty("_bingMapsKey");
            _showMapDataInEditorProperty = serializedObject.FindProperty("_showMapDataInEditor");
            _zoomLevelProperty = serializedObject.FindProperty("_zoomLevel");
            _minZoomLevelProperty = serializedObject.FindProperty("_minimumZoomLevel");
            _maxZoomLevelProperty = serializedObject.FindProperty("_maximumZoomLevel");
            _mapTerrainType = serializedObject.FindProperty("_mapTerrainType");
            _mapShapeProperty = serializedObject.FindProperty("_mapShape");
            _localMapDimensionProperty = serializedObject.FindProperty("LocalMapDimension");
            _localMapRadiusProperty = serializedObject.FindProperty("LocalMapRadius");
            _localMapHeightProperty = serializedObject.FindProperty("_localMapHeight");
            _useCustomTerrainMaterialProperty = serializedObject.FindProperty("_useCustomTerrainMaterial");
            _elevationScaleProperty = serializedObject.FindProperty("_elevationScale");
            _castShadowsProperty = serializedObject.FindProperty("_castShadows");
            _receiveShadowsProperty = serializedObject.FindProperty("_receiveShadows");
            _enableMrtkMaterialIntegrationProperty = serializedObject.FindProperty("_enableMrtkMaterialIntegration");
            _customTerrainMaterialProperty = serializedObject.FindProperty("_customTerrainMaterial");
            _isClippingVolumeWallEnabledProperty = serializedObject.FindProperty("_isClippingVolumeWallEnabled");
            _useCustomClippingVolumeMaterialProperty = serializedObject.FindProperty("_useCustomClippingVolumeMaterial");
            _customClippingVolumeMaterialProperty = serializedObject.FindProperty("_customClippingVolumeMaterial");
            _clippingVolumeDistanceTextureResolution = serializedObject.FindProperty("_clippingVolumeDistanceTextureResolution");
            _labelPrefabProperty = serializedObject.FindProperty("_labelPrefab");
            _mapEdgeColorProperty = serializedObject.FindProperty("_mapEdgeColor");
            _mapEdgeColorFadeDistanceProperty = serializedObject.FindProperty("_mapEdgeColorFadeDistance");
            _detailOffsetProperty = serializedObject.FindProperty("_detailOffset");
            _mapColliderTypeProperty = serializedObject.FindProperty("_mapColliderType");
            _maxCacheSizeInBytesProperty = serializedObject.FindProperty("_maxCacheSizeInBytes");
            _bannerWhite = (Texture2D)Resources.Load("MapsSDK-EditorBannerWhite");
            _bannerBlack = (Texture2D)Resources.Load("MapsSDK-EditorBannerBlack");
            _textureTileLayersProperty = serializedObject.FindProperty("_textureTileLayers");
            _elevationTileLayersProperty = serializedObject.FindProperty("_elevationTileLayers");
            _hideTileLayerComponentsProperty = serializedObject.FindProperty("_hideTileLayerComponents");
            _languageOverrideProperty = serializedObject.FindProperty("_languageOverride");

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
                        _startingHitPointInWorldSpace = hitInfo.Point;
                        _startingCenterInMercatorSpace = mapRenderer.Center.ToMercatorPosition();
                        _isDragging = true;
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
                        var newDeltaInMercator = new Vector2D(newDeltaInLocalSpace.x, newDeltaInLocalSpace.z) / Math.Pow(2, mapRenderer.ZoomLevel - 1);
                        var newCenter = LatLon.FromMercatorPosition(_startingCenterInMercatorSpace - newDeltaInMercator);

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

            RenderBanner();

            // Setup and key.
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("API Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            _bingMapsKeyProperty.stringValue = EditorGUILayout.PasswordField("Bing Maps Key", _bingMapsKeyProperty.stringValue);
            if (string.IsNullOrWhiteSpace(_bingMapsKeyProperty.stringValue))
            {
                Help(
                    "Provide a Bing Maps developer key to enable the map.",
                    "Sign up for a key at the Bing Maps Dev Center.",
                    "https://www.bingmapsportal.com/");
            }

            _showMapDataInEditorProperty.boolValue =
                EditorGUILayout.Toggle(
                    new GUIContent(
                        "Show Map Data in Editor",
                        "Map data usage in the editor will apply the specified Bing Maps key."),
                    _showMapDataInEditorProperty.boolValue);
            EditorGUILayout.EndVertical();

            // Location Section
            EditorGUILayout.BeginVertical(_boxStyle);
            _showMapLocationOptions = EditorGUILayout.Foldout(_showMapLocationOptions, "Location", true, _foldoutTitleStyle);
            if (_showMapLocationOptions)
            {
                var latitudeProperty = _centerProperty.FindPropertyRelative("Latitude");
                latitudeProperty.doubleValue = EditorGUILayout.DoubleField("Latitude", latitudeProperty.doubleValue);
                var longitudeProperty = _centerProperty.FindPropertyRelative("Longitude");
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
            _showMapSizingOptions = EditorGUILayout.Foldout(_showMapSizingOptions, "Map Layout", true, _foldoutTitleStyle);
            if (_showMapSizingOptions)
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
                    EditorGUILayout.LabelField(" ", "Scaled Map Dimension: " + ((MapRenderer)target).MapDimension.ToString());
                    GUILayout.Space(6f);
                    EditorGUILayout.PropertyField(_localMapHeightProperty);
                    EditorGUILayout.LabelField(" ", "Scaled Map Height: " + ((MapRenderer)target).MapHeight.ToString());
                }
                else if (_mapShapeProperty.enumValueIndex == (int)MapShape.Cylinder)
                {
                    EditorGUILayout.PropertyField(_localMapRadiusProperty);
                    EditorGUILayout.LabelField(" ", "Scaled Map Radius: " + (((MapRenderer)target).MapDimension.x / 2.0f).ToString());
                    GUILayout.Space(6f);
                    EditorGUILayout.PropertyField(_localMapHeightProperty);
                    EditorGUILayout.LabelField(" ", "Scaled Map Height: " + ((MapRenderer)target).MapHeight.ToString());
                }
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
            _terrainOptions = EditorGUILayout.Foldout(_terrainOptions, "Render Settings", true, _foldoutTitleStyle);
            if (_terrainOptions)
            {
                // Map Terrain Type Controls
                GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Map Terrain Type");
                _mapTerrainType.enumValueIndex = GUILayout.Toolbar(_mapTerrainType.enumValueIndex, _layerOptions);
                GUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(_elevationScaleProperty);
                EditorGUILayout.PropertyField(_castShadowsProperty, new GUIContent("Cast Shadows"));
                EditorGUILayout.PropertyField(_receiveShadowsProperty, new GUIContent("Receive Shadows"));
                EditorGUILayout.PropertyField(_enableMrtkMaterialIntegrationProperty, new GUIContent("Enable MRTK Integration"));
                EditorGUILayout.PropertyField(_useCustomTerrainMaterialProperty, new GUIContent("Use Custom Terrain Material"));
                if (_useCustomTerrainMaterialProperty.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_customTerrainMaterialProperty, new GUIContent("Terrain Material"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(_isClippingVolumeWallEnabledProperty, new GUIContent("Render Clipping Volume Wall"));
                if (_isClippingVolumeWallEnabledProperty.boolValue)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(_mapEdgeColorProperty, new GUIContent("Color"));
                    _mapEdgeColorFadeDistanceProperty.floatValue =
                        EditorGUILayout.Slider(new GUIContent("Edge Fade"), _mapEdgeColorFadeDistanceProperty.floatValue, 0, 1);

                    EditorGUILayout.PropertyField(
                        _useCustomClippingVolumeMaterialProperty,
                        new GUIContent("Use Custom Clipping Volume Material"));
                    if (_useCustomClippingVolumeMaterialProperty.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(_customClippingVolumeMaterialProperty, new GUIContent("Clipping Volume Material"));
                        EditorGUI.indentLevel--;
                    }

                    // Texture Camera Resolution
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Clipping Edge Resolution");
                    _clippingVolumeDistanceTextureResolution.enumValueIndex = GUILayout.Toolbar(
                        _clippingVolumeDistanceTextureResolution.enumValueIndex, _clippingVolumeDistanceTextureResolutionOptions);
                    GUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;
                }
            }
            GUILayout.Space(6f);
            EditorGUILayout.EndVertical();

            // Quality options.
            EditorGUILayout.BeginVertical(_boxStyle);
            _showQualityOptions = EditorGUILayout.Foldout(_showQualityOptions, "Quality", true, _foldoutTitleStyle);
            if (_showQualityOptions)
            {
                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                _maxCacheSizeInBytesProperty.longValue =
                    1024 *
                    1024 *
                    EditorGUILayout.LongField(
                        new GUIContent("Max Cache Size (MB)"),
                        _maxCacheSizeInBytesProperty.longValue / 1024 / 1024);
                EditorGUI.EndDisabledGroup();

                var position = EditorGUILayout.GetControlRect(false, 2 * EditorGUIUtility.singleLineHeight);
                position.height = EditorGUIUtility.singleLineHeight;

                position = EditorGUI.PrefixLabel(position, new GUIContent("Detail Offset"));
                EditorGUI.indentLevel--;

                _detailOffsetProperty.floatValue = EditorGUI.Slider(position, _detailOffsetProperty.floatValue, -1f, 1f);
                float labelWidth = position.width;

                // Render the sub-text labels.
                {
                    position.y += EditorGUIUtility.singleLineHeight;
                    position.width -= EditorGUIUtility.fieldWidth;

                    var color = GUI.color;
                    GUI.color = color * new Color(1f, 1f, 1f, 0.5f);

                    GUIStyle style =
                        new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.UpperLeft
                        };

                    EditorGUI.LabelField(position, "Low", style);

                    style.alignment = TextAnchor.UpperCenter;
                    EditorGUI.LabelField(position, "Default", style);

                    style.alignment = TextAnchor.UpperRight;
                    EditorGUI.LabelField(position, "High", style);

                    GUI.color = color;
                }

                EditorGUI.indentLevel++;
            }
            EditorGUILayout.EndVertical();

            // Texture Tile Providers
            EditorGUILayout.BeginVertical(_boxStyle);
            _showTileLayers = EditorGUILayout.Foldout(_showTileLayers, "Tile Layers", true, _foldoutTitleStyle);
            if (_showTileLayers)
            {
                EditorGUILayout.PropertyField(_textureTileLayersProperty, true);
                EditorGUILayout.PropertyField(_elevationTileLayersProperty, true);
                EditorGUILayout.PropertyField(_hideTileLayerComponentsProperty);
                GUILayout.Space(12f);
            }
            EditorGUILayout.EndVertical();

            // Localization
            EditorGUILayout.BeginVertical(_boxStyle);
            _showLocalizationOptions = EditorGUILayout.Foldout(_showTileLayers, "Localization", true, _foldoutTitleStyle);
            if (_showLocalizationOptions)
            {
                var previousIsLanguageAutoDetected = _languageOverrideProperty.intValue == (int)SystemLanguage.Unknown;
                var newIsLanguageAutoDetected = EditorGUILayout.Toggle("Autodetect Language", previousIsLanguageAutoDetected);

                // If we are switching from autodetected to override, initialize override property with the current system language.
                if (!newIsLanguageAutoDetected && previousIsLanguageAutoDetected)
                {
                    _languageOverrideProperty.intValue = (int)Application.systemLanguage;
                }

                // If we are switching from overridden to autodetected, clear the override property to unknown.
                if (newIsLanguageAutoDetected && !previousIsLanguageAutoDetected)
                {
                    _languageOverrideProperty.intValue = (int)SystemLanguage.Unknown;
                }

                if (newIsLanguageAutoDetected)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.EnumPopup("Language", Application.systemLanguage);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.PropertyField(_languageOverrideProperty, new GUIContent("Language"));
                }

            }
            EditorGUILayout.EndVertical();


            GUILayout.Space(4);
            serializedObject.ApplyModifiedProperties();
        }

        private void Initialize()
        {
            if (_baseStyle == null)
            {
                _baseStyle = new GUIStyle
                {
                    wordWrap = true,
                    font = EditorStyles.helpBox.font,
                    fontSize = EditorStyles.helpBox.fontSize,
                    normal = EditorStyles.helpBox.normal
                };
                _baseStyle.normal.background = null;
                _baseStyle.stretchWidth = false;
                _baseStyle.stretchHeight = false;
                _baseStyle.margin = new RectOffset();
            }

            if (_errorIcon == null)
            {
                _errorIcon = EditorGUIUtility.TrIconContent("console.erroricon");
            }

            if (_errorIconStyle == null)
            {
                _errorIconStyle =
                    new GUIStyle(_baseStyle)
                    {
                        stretchHeight = false,
                        alignment = TextAnchor.MiddleLeft,
                        fixedWidth = _errorIcon.image.width,
                        fixedHeight = 1.0f * _errorIcon.image.height,
                        stretchWidth = false,
                        wordWrap = false
                    };
            }

            if (_hyperlinkStyle == null)
            {
                _hyperlinkStyle = new GUIStyle(_baseStyle);
                _hyperlinkStyle.alignment = TextAnchor.UpperLeft;
                _hyperlinkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
                _hyperlinkStyle.stretchWidth = false;
                _hyperlinkStyle.padding = new RectOffset();
                _hyperlinkStyle.alignment = TextAnchor.UpperLeft;
            }

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
        }

        private void RenderBanner()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(EditorGUIUtility.isProSkin ? _bannerWhite : _bannerBlack, GUILayout.MaxHeight(96f));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void Help(string message, string urlMessage, string url)
        {
            EditorGUI.indentLevel--;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            var rect = EditorGUILayout.BeginHorizontal();
            {
                var iconWidth = _errorIcon.image.width;
                EditorGUILayout.LabelField(_errorIcon, _errorIconStyle, GUILayout.Width(iconWidth));

                EditorGUILayout.BeginVertical();
                {
                    GUILayout.Space(8);
                    EditorGUILayout.LabelField(message, _baseStyle);

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(urlMessage, _hyperlinkStyle);

                        var linkRect = GUILayoutUtility.GetLastRect();

                        if (Event.current.type == EventType.Repaint)
                        {
                            EditorGUIUtility.AddCursorRect(linkRect, MouseCursor.Link);
                        }

                        if (Event.current.type == EventType.MouseUp && linkRect.Contains(Event.current.mousePosition))
                        {
                            Application.OpenURL(url);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(4);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel++;
        }

        private void QueuePlayerLoopUpdate()
        {
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
