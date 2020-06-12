// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEditor;

    [CustomEditor(typeof(ClusterMapPin))]
    [CanEditMultipleObjects]
    class MapClusterPinEditor : Editor
    {
        private SerializedProperty _useRealworldScaleProperty;
        private SerializedProperty _scaleCurveProperty;
        private SerializedProperty _isLayerSynchronizedProperty;

        internal void OnEnable()
        {
            _scaleCurveProperty = serializedObject.FindProperty("ScaleCurve");
            _useRealworldScaleProperty = serializedObject.FindProperty("UseRealWorldScale");
            _isLayerSynchronizedProperty = serializedObject.FindProperty("IsLayerSynchronized");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(_useRealworldScaleProperty);
            EditorGUILayout.PropertyField(_scaleCurveProperty);
            EditorGUILayout.PropertyField(_isLayerSynchronizedProperty);

            EditorGUI.BeginDisabledGroup(true);
            var clusterMapPin = (ClusterMapPin)target;
            var latLon = clusterMapPin.MercatorCoordinate.ToLatLon();
            EditorGUILayout.TextField("Location", latLon.LatitudeInDegrees + ", " + latLon.LongitudeInDegrees);
            EditorGUILayout.IntField("Size", clusterMapPin.Size);
            EditorGUILayout.IntField("Level Of Detail", clusterMapPin.LevelOfDetail);
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
