// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(MapSession))]
    [CanEditMultipleObjects]
    internal class MapSessionEditor : Editor
    {
        private SerializedProperty _developerKeyProperty;
        private SerializedProperty _showMapDataInEditorProperty;

        private static bool _showLocalizationOptions = true;
        private SerializedProperty _languageOverrideProperty;
        private SerializedProperty _languageChangedProperty;
        private bool _autodetectRegion;
        private SerializedProperty _regionOverrideProperty;

        private GUIStyle _baseStyle = null;
        private GUIStyle _hyperlinkStyle = null;
        private GUIStyle _iconStyle = null;
        private GUIContent _warnIcon = null;
        private GUIStyle _foldoutTitleStyle = null;
        private GUIStyle _boxStyle = null;
        private Texture2D _bannerWhite = null;
        private Texture2D _bannerBlack = null;

        private void OnEnable()
        {
            _developerKeyProperty = serializedObject.FindProperty("_developerKey");
            _showMapDataInEditorProperty = serializedObject.FindProperty("_showMapDataInEditor");
            _languageOverrideProperty = serializedObject.FindProperty("_languageOverride");
            _languageChangedProperty = serializedObject.FindProperty("_languageChanged");
            _regionOverrideProperty = serializedObject.FindProperty("_regionOverride");
            _autodetectRegion = string.IsNullOrWhiteSpace(_regionOverrideProperty.stringValue);
            _bannerWhite = (Texture2D)Resources.Load("MapsSDK-EditorBannerWhite");
            _bannerBlack = (Texture2D)Resources.Load("MapsSDK-EditorBannerBlack");
        }

        public override void OnInspectorGUI()
        {
            Initialize();

            serializedObject.UpdateIfRequiredOrScript();

            var overrideRegion = !_autodetectRegion || !string.IsNullOrWhiteSpace(_regionOverrideProperty.stringValue);
            _autodetectRegion = !overrideRegion;

            // This should just be done one time.
            {
                var mapSession = target as MapSession;
                var components = mapSession.gameObject.GetComponents<Component>();

                var mapRendererIndex = -1;
                for (var i = 0; i < components.Length; i++)
                {
                    if (components[i] is MapRenderer)
                    {
                        mapRendererIndex = i;
                        break;
                    }
                }

                var mapSessionIndex = -1;
                for (var i = 0; i < components.Length; i++)
                {
                    if (components[i] is MapSession)
                    {
                        mapSessionIndex = i;
                        break;
                    }
                }

                if (mapSessionIndex > mapRendererIndex)
                {
                    var delta = mapSessionIndex - mapRendererIndex;
                    while (delta > 0)
                    {
                        UnityEditorInternal.ComponentUtility.MoveComponentUp(mapSession);
                        delta--;
                    }
                }
            }

            RenderBanner();

            // Setup and key.
            _developerKeyProperty.stringValue = EditorGUILayout.PasswordField("Developer Key", _developerKeyProperty.stringValue);
            if (string.IsNullOrWhiteSpace(_developerKeyProperty.stringValue))
            {
                EditorGUI.indentLevel++;
                Help(
                    "Provide a developer key to enable Bing Maps services.",
                    "Sign up for a key at the Bing Maps Dev Center.",
                    "https://www.bingmapsportal.com/");
                EditorGUI.indentLevel--;
            }

            _showMapDataInEditorProperty.boolValue =
                EditorGUILayout.Toggle(
                    new GUIContent(
                        "Show Map Data in Editor",
                        "Map data usage in the editor will apply to the specified developer key."),
                    _showMapDataInEditorProperty.boolValue);

            EditorGUI.indentLevel++;

            // Localization
            EditorGUILayout.BeginVertical(_boxStyle);
            _showLocalizationOptions = EditorGUILayout.Foldout(_showLocalizationOptions, "Localization", true, _foldoutTitleStyle);
            if (_showLocalizationOptions)
            {
                _autodetectRegion = EditorGUILayout.Toggle("Autodetect Region", _autodetectRegion);
                EditorGUI.BeginDisabledGroup(_autodetectRegion);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_regionOverrideProperty);
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();

                if (_autodetectRegion)
                {
                    _regionOverrideProperty.stringValue = string.Empty;
                }

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

                EditorGUI.indentLevel++;
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
                EditorGUI.indentLevel--;

                GUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(12);
                EditorGUILayout.PropertyField(_languageChangedProperty);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }

        private void Help(string message, string urlMessage, string url)
        {
            EditorGUI.indentLevel--;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                GUILayout.Space(8);
                var iconWidth = _warnIcon.image.width;
                EditorGUILayout.LabelField(_warnIcon, _iconStyle, GUILayout.Width(iconWidth));
                EditorGUILayout.EndVertical();

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

            if (_warnIcon == null)
            {
                _warnIcon = EditorGUIUtility.TrIconContent("console.warnicon");
            }

            if (_iconStyle == null)
            {
                _iconStyle =
                    new GUIStyle(_baseStyle)
                    {
                        stretchHeight = false,
                        alignment = TextAnchor.MiddleLeft,
                        fixedWidth = _warnIcon.image.width,
                        fixedHeight = _warnIcon.image.height,
                        stretchWidth = false,
                        wordWrap = false
                    };
            }

            if (_hyperlinkStyle == null)
            {
                _hyperlinkStyle =
                    new GUIStyle(_baseStyle)
                    {
                        alignment = TextAnchor.UpperLeft
                    };
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
    }
}
