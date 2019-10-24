// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// This drawer will be used for anything deriving from <see cref="TileLayer"/>.
    /// It manages showing drawers for the derived objects if they have
    /// the <see cref="CustomTileLayerDrawer"/> attribute.
    /// </summary>
    [CustomPropertyDrawer(typeof(TileLayer), true)]
    public class TileLayerDrawer : PropertyDrawer
    {
        /// <summary>
        /// A dictionary of <see cref="PropertyDrawer"/> instances for each 
        /// <see cref="Type"/> that has the <see cref="CustomTileLayerDrawer"/> 
        /// attribute.
        /// </summary>
        private Lazy<Dictionary<Type, PropertyDrawer>> _propertyDrawerCache = new Lazy<Dictionary<Type, PropertyDrawer>>(() =>
        {
            var result = new Dictionary<Type, PropertyDrawer>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    foreach (var attribute in type.GetCustomAttributes<CustomTileLayerDrawer>())
                    {
                        if (typeof(PropertyDrawer).IsAssignableFrom(type))
                        {
                            result[attribute.TargetType] = (PropertyDrawer)Activator.CreateInstance(type);
                        }
                    }
                }
            }

            return result;
        });

        private ScriptableObjectDrawer _scriptableObjectDrawer = new ScriptableObjectDrawer();

        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.objectReferenceValue != null &&
                _propertyDrawerCache.Value.TryGetValue(property.objectReferenceValue.GetType(), out var propertyDrawer))
            {
                // Property has a drawer... use that to get the height.
                return propertyDrawer.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing;
            }
            else
            {
                // Property does not have a drawer... leave space for the type name.
                return _scriptableObjectDrawer.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.objectReferenceValue != null)
            {
                if (_propertyDrawerCache.Value.TryGetValue(property.objectReferenceValue.GetType(), out var propertyDrawer))
                {
                    // Property has a drawer... use that to display.
                    propertyDrawer.OnGUI(position, property, label);
                }
                else
                {
                    // Property does not have a drawer, use a draw that shows all the serialized fields.
                    _scriptableObjectDrawer.OnGUI(position, property, label);
                }
            }
            else
            {
                EditorGUI.LabelField(position, "Unknown");
            }
        }
    }
}
