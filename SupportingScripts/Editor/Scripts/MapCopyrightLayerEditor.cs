// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEditor;

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

        internal void OnEnable()
        {
            _fontProperty = serializedObject.FindProperty("_font");
            _textColorProperty = serializedObject.FindProperty("_textColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            var mapCopyrightLayer = (MapCopyrightLayer)target;
            if (!mapCopyrightLayer.enabled)
            {
                EditorGUILayout.HelpBox(WarningMessageText, MessageType.Warning);
            }

            EditorGUILayout.PropertyField(_fontProperty, true);
            EditorGUILayout.PropertyField(_textColorProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
