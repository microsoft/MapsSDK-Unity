// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// A custom editor for the <see cref="DefaultTextureTileLayerDrawer"/> which will be displayed within
    /// the <see cref="TextureTileLayerListDrawer"/>.
    /// </summary>
    [CustomTileLayerDrawer(typeof(DefaultTextureTileLayer))]
    public class DefaultTextureTileLayerDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var isSymbolic = false;
            using (var serializedObject = new SerializedObject(property.objectReferenceValue))
            {
                var imageryTypeProperty = serializedObject.FindProperty("_imageryType");
                isSymbolic = imageryTypeProperty.enumValueIndex == 0;
            }

            return
                // At a minimum, show 3 lines.
                3 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) +
                // Additionally, we may show 2 more fields and a header for symbolic specific options.
                (isSymbolic ?
                    3 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) :
                    0);
        }

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (var serializedObject = new SerializedObject(property.objectReferenceValue))
            {
                var imageryTypeProperty = serializedObject.FindProperty("_imageryType");
                EditorGUI.PropertyField(position, imageryTypeProperty);
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(position, serializedObject.FindProperty("_areRoadsEnabled"), new GUIContent("Show Roads"));
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(position, serializedObject.FindProperty("_areLabelsEnabled"), new GUIContent("Show Labels"));
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if (imageryTypeProperty.enumValueIndex == 0)
                {
                    // This is symbolic imagery. Show additional options.
                    EditorGUI.LabelField(position, "Additional Options", EditorStyles.boldLabel);

                    EditorGUI.indentLevel++;

                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(position, serializedObject.FindProperty("_isHillShadingEnabled"), new GUIContent("Show Hill Shading"));
                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(position, serializedObject.FindProperty("_imageryStyle"));

                    EditorGUI.indentLevel--;

                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
