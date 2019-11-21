// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEditor;

    [CustomEditor(typeof(MapPinLayer))]
    [CanEditMultipleObjects]
    class MapPinLayerEditor : Editor
    {
        private SerializedProperty _layerNameProperty;
        private SerializedProperty _isClusteringEnabledProperty;
        private SerializedProperty _clusterThresholdProperty;
        private SerializedProperty _mapPinClusterPrefabProperty;

        internal void OnEnable()
        {
            _layerNameProperty = serializedObject.FindProperty("_layerName");
            _isClusteringEnabledProperty = serializedObject.FindProperty("_isClusteringEnabled");
            _clusterThresholdProperty = serializedObject.FindProperty("_clusterThreshold");
            _mapPinClusterPrefabProperty = serializedObject.FindProperty("_clusterMapPinPrefab");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(_layerNameProperty);
            EditorGUILayout.PropertyField(_isClusteringEnabledProperty);
            if (_isClusteringEnabledProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_clusterThresholdProperty);
                EditorGUILayout.PropertyField(_mapPinClusterPrefabProperty);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
