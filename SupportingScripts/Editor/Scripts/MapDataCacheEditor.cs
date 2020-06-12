// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(MapDataCache))]
    internal class MapDataCacheEditor : Editor
    {
        private SerializedProperty _percentUtilizationProperty;

        private void OnEnable()
        {
            _percentUtilizationProperty = serializedObject.FindProperty("_percentUtilization");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            var mapDataCache = (MapDataCache)target;
            mapDataCache.MaxCacheSizeInBytes =
                1024 *
                1024 *
                EditorGUILayout.LongField(
                    new GUIContent("Max Cache Size (MB)"),
                    mapDataCache.MaxCacheSizeInBytes / 1024 / 1024);

            EditorGUILayout.PropertyField(_percentUtilizationProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
