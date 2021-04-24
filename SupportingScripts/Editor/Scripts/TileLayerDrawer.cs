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
    /// It manages showing drawers for the derived objects if they have the <see cref="CustomTileLayerDrawer"/> attribute.
    /// </summary>
    [CustomPropertyDrawer(typeof(TileLayer), true)]
    public class TileLayerDrawer : PropertyDrawer
    {
        /// <summary>
        /// A dictionary of <see cref="PropertyDrawer"/> instances for each 
        /// <see cref="Type"/> that has the <see cref="CustomTileLayerDrawer"/> 
        /// attribute.
        /// </summary>
        private readonly Lazy<Dictionary<Type, PropertyDrawer>> _propertyDrawerCache =
            new Lazy<Dictionary<Type, PropertyDrawer>>(
                () =>
                {
                    var result = new Dictionary<Type, PropertyDrawer>();

                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
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
                        catch (ReflectionTypeLoadException e)
                        {
                            Debug.LogWarning($"{nameof(TileLayerDrawer)}: Unable to load types from {assembly.FullName}\r\n{e}");
                        }
                    }

                    return result;
                });

        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.objectReferenceValue != null &&
                _propertyDrawerCache.Value.TryGetValue(property.objectReferenceValue.GetType(), out var propertyDrawer))
            {
                // Property has a drawer... use that to get the height and add on the header height.
                return
                    EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing +
                    propertyDrawer.GetPropertyHeight(property, label) +
                    EditorGUIUtility.standardVerticalSpacing;
            }
            else
            {
                // Property does not have a drawer... leave space for the header.
                float totalHeight = EditorGUIUtility.standardVerticalSpacing;

                totalHeight += EditorGUIUtility.singleLineHeight;

                if (property.objectReferenceValue == null)
                {
                    return totalHeight;
                }

                SerializedObject targetObject = new SerializedObject(property.objectReferenceValue);

                if (targetObject == null)
                {
                    return totalHeight;
                }

                SerializedProperty field = targetObject.GetIterator();

                field.NextVisible(true);

                while (field.NextVisible(false))
                {
                    totalHeight += EditorGUI.GetPropertyHeight(field, true) + EditorGUIUtility.standardVerticalSpacing;
                }

                return totalHeight;
            }
        }

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.objectReferenceValue != null)
            {
                using (var serializedObject = new SerializedObject(property.objectReferenceValue))
                {
                    // First, render a header for the TileLayer.

                    if (serializedObject == null)
                    {
                        EditorGUI.LabelField(position, property.objectReferenceValue.GetType().Name);
                        return;
                    }

                    var tileLayer = (TileLayer)property.GetValue();
                    if (tileLayer == null)
                    {
                        return;
                    }

                    var script = MonoScript.FromMonoBehaviour(tileLayer);

                    var tileLayerNamePosition = position;
                    EditorGUILayout.BeginHorizontal();
                    {
                        Undo.RecordObject(serializedObject.targetObject, "Enabled");
                        var iconPosition = position;
                        iconPosition.height = EditorGUIUtility.singleLineHeight;
                        iconPosition.width = iconPosition.height;
                        GUI.Box(iconPosition, AssetPreview.GetMiniThumbnail(script), GUIStyle.none);

                        var togglePosition = iconPosition;
                        togglePosition.x += 4 + EditorStyles.toggle.padding.left;
                        togglePosition.width = EditorStyles.toggle.padding.left;
                        var customToggleStyle = new GUIStyle(EditorStyles.toggle) { alignment = TextAnchor.MiddleCenter };
                        tileLayer.enabled = EditorGUI.Toggle(togglePosition, tileLayer.enabled, customToggleStyle);
                        serializedObject.Update();

                        var name = ObjectNames.NicifyVariableName(script.name) + " (Script)";
                        var labelSize = EditorStyles.boldLabel.CalcSize(new GUIContent(name));
                        tileLayerNamePosition.height = EditorGUIUtility.singleLineHeight;
                        tileLayerNamePosition.x += 4 + EditorStyles.toggle.padding.left;
                        tileLayerNamePosition.width = labelSize.x + EditorStyles.toggle.padding.left;
                        EditorGUI.LabelField(tileLayerNamePosition, new GUIContent(name), EditorStyles.boldLabel);
                    }
                    EditorGUILayout.EndHorizontal();

                    var fieldRect = new Rect(position) { height = EditorGUIUtility.singleLineHeight };
                    var propertyRects = new List<Rect>();
                    fieldRect.xMin += 5;
                    var marchingRect = new Rect(fieldRect);
                    marchingRect.y += marchingRect.height + EditorGUIUtility.standardVerticalSpacing;

                    // For the content of the TileLayer, either automatically generate UI for the fields,
                    // or defer to the CustomTileLayerDrawer.

                    EditorGUI.indentLevel++;

                    if (_propertyDrawerCache.Value.TryGetValue(property.objectReferenceValue.GetType(), out var propertyDrawer))
                    {
                        // Property has a drawer... use that to display.
                        propertyDrawer.OnGUI(marchingRect, property, label);
                    }
                    else
                    {
                        // Property does not have a drawer, use a draw that shows all the serialized fields.
                        var field = serializedObject.GetIterator();
                        field.NextVisible(true);

                        while (field.NextVisible(false))
                        {
                            marchingRect.height = EditorGUI.GetPropertyHeight(field, true);
                            propertyRects.Add(marchingRect);
                            marchingRect.y += marchingRect.height + EditorGUIUtility.standardVerticalSpacing;
                        }

                        int index = 0;
                        field = serializedObject.GetIterator();
                        field.NextVisible(true);

                        //Replacement for "editor.OnInspectorGUI ();" so we have more control on how we draw the editor
                        while (field.NextVisible(false))
                        {
                            try
                            {
                                EditorGUI.PropertyField(propertyRects[index], field, true);
                            }
                            catch (StackOverflowException)
                            {
                                field.objectReferenceValue = null;
                                Debug.LogError(
                                    "Detected self-nesting causing a StackOverflowException, avoid using the same object inside a nested structure.");
                            }

                            ++index;
                        }
                    }

                    serializedObject.ApplyModifiedProperties();

                    EditorGUI.indentLevel--;

                    // Shows context menu for editing the associated script, i.e. will open text editor like Visual Studio.
                    var currentEvent = Event.current;
                    if (currentEvent.type == EventType.ContextClick)
                    {
                        if (tileLayerNamePosition.Contains(currentEvent.mousePosition))
                        {
                            if (script != null)
                            {
                                GenericMenu menu = new GenericMenu();
                                menu.AddItem(new GUIContent("Edit Script"), false, () => AssetDatabase.OpenAsset(script));
                                menu.ShowAsContext();
                            }

                        }
                    }
                }
            }
            else
            {
                EditorGUI.LabelField(position, "Unknown");
            }
        }
    }
}
