// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEditor;

    [CustomEditor(typeof(MapContourLineLayer))]
    [CanEditMultipleObjects]
    class MapContourLineLayerEditor : Editor
    {
        private SerializedProperty _majorIntervalAltitudeInMetersProperty;
        private SerializedProperty _numMinorIntervalSectionsProperty;
        private SerializedProperty _majorColorProperty;
        private SerializedProperty _minorColorProperty;
        private SerializedProperty _majorLinePixelSizeProperty;
        private SerializedProperty _minorLinePixelSizeProperty;

        internal void OnEnable()
        {
            _majorIntervalAltitudeInMetersProperty = serializedObject.FindProperty("_majorIntervalAltitudeInMeters");
            _numMinorIntervalSectionsProperty = serializedObject.FindProperty("_numMinorIntervalSections");
            _majorColorProperty = serializedObject.FindProperty("_majorColor");
            _minorColorProperty = serializedObject.FindProperty("_minorColor");
            _majorLinePixelSizeProperty = serializedObject.FindProperty("_majorLinePixelSize");
            _minorLinePixelSizeProperty = serializedObject.FindProperty("_minorLinePixelSize");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.LabelField("Interval", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_majorIntervalAltitudeInMetersProperty);
            EditorGUILayout.PropertyField(_numMinorIntervalSectionsProperty);
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Render Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_majorColorProperty);
            EditorGUILayout.PropertyField(_majorLinePixelSizeProperty);
            EditorGUILayout.PropertyField(_minorColorProperty);
            EditorGUILayout.PropertyField(_minorLinePixelSizeProperty);
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
