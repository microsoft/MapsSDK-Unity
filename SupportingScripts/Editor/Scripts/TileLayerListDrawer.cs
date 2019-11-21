// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    /// <summary>
    /// Base class for a <see cref="PropertyDrawer"/> that can be used to display
    /// editor UI for a list of <see cref="TileLayer"/> instances.
    /// </summary>
    public abstract class TileLayerListDrawer<TLayer> : PropertyDrawer where TLayer : TileLayer
    {
        /// <summary>
        /// Derived classes may implement this to limit the number of entries that can be added to the list.
        /// </summary>
        protected virtual int MaximumCount { get; } = int.MaxValue;

        /// <summary>
        /// Derived classes must implement this to provide the string that is shown at the top of the list.
        /// </summary>
        protected abstract string Title { get; }

        private ReorderableList _reorderableList;

        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetList(property).GetHeight();
        }

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GetList(property).DoList(EditorGUI.IndentedRect(position));
        }

        private ReorderableList GetList(SerializedProperty tileLayerListProperty)
        {
            if (_reorderableList == null)
            {
                var internalListProperty = tileLayerListProperty.FindPropertyRelative("_list");
                var tileLayerInternalList = (List<TLayer>)internalListProperty.GetValue();
                var tileLayerList = (TileLayerList<TLayer>)tileLayerListProperty.GetValue();
                if (internalListProperty == null || tileLayerList == null || tileLayerInternalList == null)
                {
                    throw new NotSupportedException(
                        $"The property {tileLayerListProperty.name} of type {tileLayerListProperty.type} must be a TileLayerList.");
                }

                // Object that owns the list property (MapRenderer).
                var ownerObject = tileLayerListProperty.serializedObject;

                _reorderableList =
                    new ReorderableList(
                        ownerObject,
                        internalListProperty,
                        draggable: true,
                        displayHeader: true,
                        displayAddButton: true,
                        displayRemoveButton: true)
                    {
                        drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, Title),

                        elementHeightCallback = (int index) => EditorGUI.GetPropertyHeight(internalListProperty.GetArrayElementAtIndex(index)),

                        // The full type name would be displayed if we didn't override this and show the shorter one.
                        drawElementCallback =
                            (Rect rect, int index, bool isActive, bool isFocused) =>
                            {
                                rect.width -= 40;
                                SerializedProperty serializedElement = internalListProperty.GetArrayElementAtIndex(index);
                                EditorGUI.PropertyField(rect, serializedElement);
                            },

                        onCanAddCallback = (ReorderableList reorderableList) => reorderableList.count < MaximumCount,

                        onRemoveCallback =
                            (ReorderableList reorderableList) =>
                            {
                                var tileLayer = tileLayerList[reorderableList.index];
                                if (tileLayer != null)
                                {
                                    // This will cause the tileLayer to remove itself from the list, internally.
                                    Undo.DestroyObjectImmediate(tileLayer);
                                }
                                else
                                {
                                    // If the item is null (some invalid/broken component),
                                    // remove it directly from the list. ReorderableList doesn't.
                                    Undo.RecordObject(ownerObject.targetObject, "Remove tile layer from list");
                                    tileLayerInternalList.RemoveAt(reorderableList.index);
                                }

                                // Commit changes to the MapRenderer back to the SerializedObject.
                                ownerObject.Update();
                            },

                        onAddDropdownCallback =
                            (Rect buttonRect, ReorderableList reorderableList) =>
                            {
                                // Get all the eligible types.
                                GetValidTileLayerComponents(internalListProperty, out var types, out var typeNames);

                                // Create the menu.
                                var menu = new GenericMenu();
                                for (int typeIndex = 0; typeIndex < typeNames.Length; ++typeIndex)
                                {
                                    menu.AddItem(
                                        // Display the shorter name
                                        new GUIContent(typeNames[typeIndex]),
                                        false,
                                        (userDataTypeIndex) =>
                                        {
                                            var mapRenderer = (MapRenderer)ownerObject.targetObject;
                                            var newLayer = (TLayer)Undo.AddComponent(mapRenderer.gameObject, types[(int)userDataTypeIndex]);
                                            if (newLayer == null)
                                            {
                                                // Maybe show a message box to indicate the layer was unable to be added. This shouldn't
                                                // happen though since we already filter out types that have already been added with
                                                // DisallowMultipleComponent. But maybe there are other reasons component creation fails?
                                            }

                                            // Commit changes to the MapRenderer back to the SerializedObject.
                                            ownerObject.Update();
                                        },
                                        userData: typeIndex);
                                }

                                if (typeNames.Length == 0)
                                {
                                    menu.AddDisabledItem(new GUIContent("None"));
                                }

                                menu.ShowAsContext();
                            }
                    };
            }

            return _reorderableList;
        }

        private void GetValidTileLayerComponents(
            SerializedProperty existingTypeList,
            out List<Type> types,
            out string[] typeNames)
        {
            // First, use Unity's "Unsupported" API to get a list of all valid Component types. The commands return only IDs of the
            // Components that are valid, i.e. are in a script with the correct corresponding filename, or come from an assembly. If
            // we were to rely only on reflected Types it's possible to get MonoBehaviors that are invalid to add from the Editor due
            // to not following these constraints. Filter to the ones which are just TileLayers...
            var commands = Unsupported.GetSubmenusCommands("Component");
            types = new List<Type>();
            for (var i = 0; i < commands.Length; i++)
            {
                var command = commands[i];
                if (command.StartsWith("SCRIPT"))
                {
                    var scriptIdString = command.Substring(6);
                    if (int.TryParse(scriptIdString, out var scriptId))
                    {
                        var script = EditorUtility.InstanceIDToObject(scriptId);
                        if (script is MonoScript monoScript)
                        {
                            var type = monoScript.GetClass();
                            if (type != null && type.IsSubclassOf(typeof(TLayer)))
                            {
                                types.Add(type);
                            }
                        }
                    }
                }
            }

            // Remove any types that are already in the list and have the DisallowMultipleComponent on them.
            for (var i = 0; i < existingTypeList.arraySize; ++i)
            {
                var objectReferenceValue = existingTypeList.GetArrayElementAtIndex(i).objectReferenceValue;
                if (objectReferenceValue != null)
                {
                    var elementType = objectReferenceValue.GetType();
                    if (elementType.GetCustomAttributes(typeof(DisallowMultipleComponent), true).Length > 0)
                    {
                        types.RemoveAll(type => type == elementType);
                    }
                }
            }

            // Build a list of the simple type name for display.
            typeNames = new string[types.Count];
            for (var i = 0; i < types.Count; ++i)
            {
                typeNames[i] = types[i].Name;
            }
        }
    }
}
