// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEditor;

    [CustomEditor(typeof(ClusterMapPin))]
    [CanEditMultipleObjects]
    class MapClusterPinEditor : Editor
    {
        private SerializedProperty _scaleCurveProperty;
        private SerializedProperty _useRealworldScaleProperty;

        internal void OnEnable()
        {
            _scaleCurveProperty = serializedObject.FindProperty("ScaleCurve");
            _useRealworldScaleProperty = serializedObject.FindProperty("UseRealWorldScale");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(_useRealworldScaleProperty);
            EditorGUILayout.PropertyField(_scaleCurveProperty);

            EditorGUI.BeginDisabledGroup(true);
            var clusterMapPin = (ClusterMapPin)target;
            EditorGUILayout.IntField("Size", clusterMapPin.Size);
            EditorGUILayout.IntField("Level Of Detail", clusterMapPin.LevelOfDetail);
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
