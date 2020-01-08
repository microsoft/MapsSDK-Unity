// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(MapCopyrightLayer))]
    [CanEditMultipleObjects]
    class MapCopyrightLayerEditor : Editor
    {
        private const string WarningMessageText =
            "If the MapCopyrightLayer has been disabled, the copyright text must be displayed manually by retrieving the copyright string " +
            "from the MapRenderer’s Copyright property and rendering it with a component like TextMesh or TextMeshPro.\r\n\r\n" + 
            "The copyright text must be displayed in a conspicuous manner, near in proximity to the map.";

        private SerializedProperty _fontProperty;
        private SerializedProperty _textColorProperty;
        private SerializedProperty _mapCopyrightAlignmentProperty;
        private readonly GUIContent[] _mapCopyrightAlignmentPropertyOptions = new GUIContent[]
        {
            new GUIContent("Bottom", "Default alignment. Copyright text is rendered at the bottom of the map."),
            new GUIContent("Top", "Copyright text is rendered at the bottom of the map."),
        };

        internal void OnEnable()
        {
            _fontProperty = serializedObject.FindProperty("_font");
            _textColorProperty = serializedObject.FindProperty("_textColor");
            _mapCopyrightAlignmentProperty = serializedObject.FindProperty("_mapCopyrightAlignment");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            GUILayout.Space(8);
            var mapCopyrightLayer = (MapCopyrightLayer)target;
            if (!mapCopyrightLayer.enabled)
            {
                EditorGUILayout.HelpBox(WarningMessageText, MessageType.Warning);
            }
            EditorGUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Text Alignment");
                _mapCopyrightAlignmentProperty.enumValueIndex = GUILayout.Toolbar(
                    _mapCopyrightAlignmentProperty.enumValueIndex, _mapCopyrightAlignmentPropertyOptions);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.PropertyField(_fontProperty, true);
            EditorGUILayout.PropertyField(_textColorProperty);
            EditorGUILayout.EndVertical();
            GUILayout.Space(4);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
