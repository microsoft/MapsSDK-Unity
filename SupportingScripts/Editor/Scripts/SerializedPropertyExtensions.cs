// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Extension class for SerializedProperties.
    /// </summary>
    public static class SerializedPropertyExtensions
    {
        /// <summary>
        /// Get the object the serialized property holds by using reflection.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static object GetValue(this SerializedProperty property)
        {
            var obj = UnitySerializationPath.GetNestedObject(property.propertyPath, GetSerializedPropertyRootComponent(property), out string fieldName);
            return UnitySerializationPath.GetFieldOrPropertyValue(fieldName, obj, true);
        }

        /// <summary>
        /// Get the component of a serialized property
        /// </summary>
        /// <param name="property">The property that is part of the component</param>
        /// <returns>The root component of the property</returns>
        public static Component GetSerializedPropertyRootComponent(SerializedProperty property)
        {
            return (Component)property.serializedObject.targetObject;
        }
    }
}
