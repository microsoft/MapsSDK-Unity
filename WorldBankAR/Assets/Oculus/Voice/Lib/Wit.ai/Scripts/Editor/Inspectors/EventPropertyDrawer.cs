/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Meta.WitAi.Events.Editor
{
    public abstract class EventPropertyDrawer<T> : PropertyDrawer
    {
        private const int CONTROL_SPACING = 5;
        private const int UNSELECTED = -1;
        private const int BUTTON_WIDTH = 75;
        private const int PROPERTY_FIELD_SPACING = 25;

        private bool showEvents = false;

        private int selectedCategoryIndex = 0;
        private int selectedEventIndex = 0;

        private int propertyOffset;

        private static Dictionary<string, List<string>> eventCategories;

        private void InitializeEventCategories(Type eventsType)
        {
            EventCategoryAttribute[] eventCategoryAttributes;

            eventCategories = new Dictionary<string, List<string>>();

            foreach (var field in eventsType.GetFields())
            {
                eventCategoryAttributes = field.GetCustomAttributes(
                    typeof(EventCategoryAttribute), false) as EventCategoryAttribute[];

                if (eventCategoryAttributes != null && eventCategoryAttributes.Length != 0)
                {
                    foreach (var eventCategory in eventCategoryAttributes)
                    {
                        if (!eventCategories.TryGetValue(eventCategory.Category, out var categories))
                        {
                            eventCategories[eventCategory.Category] = new List<string>();
                        }

                        eventCategories[eventCategory.Category].Add(field.Name);
                    }
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var eventObject = fieldInfo.GetValue(property.serializedObject.targetObject) as EventRegistry;

            var lineHeight = EditorGUIUtility.singleLineHeight;
            var lines = 1;
            var height = 0;

            // Allocate enough lines to display dropdown elements depending on which ones are showing.
            if (showEvents && Selection.activeTransform)
                lines++;

            if (showEvents && selectedCategoryIndex != UNSELECTED)
                lines++;

            height = Mathf.RoundToInt(lineHeight * lines);

            // By default, the property elements appear directly below the dropdowns.
            propertyOffset = height + (int)WitStyles.TextButtonPadding;

            // If the Events foldout is expanded and there are overridden properties, allocate space for them.
            if (eventObject != null && eventObject.OverriddenCallbacks.Count != 0 && showEvents)
            {
                var callbacksArray = eventObject.OverriddenCallbacks.ToArray();

                foreach (var callback in callbacksArray)
                {
                    height += Mathf.RoundToInt(EditorGUI.GetPropertyHeight(property.FindPropertyRelative(callback),
                                                   true) + CONTROL_SPACING);
                }

                // Add some extra space so the last property field's +/- buttons don't overlap the next control.
                height += PROPERTY_FIELD_SPACING;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            showEvents = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), showEvents, "Events");

            if (showEvents && Selection.activeTransform)
            {
                if (eventCategories == null)
                    InitializeEventCategories(fieldInfo.FieldType);

                var eventObject = fieldInfo.GetValue(property.serializedObject.targetObject) as EventRegistry;

                var eventCategoriesKeyArray = eventCategories.Keys.ToArray();

                EditorGUI.indentLevel++;

                // Shift the control rectangle down one line to accomodate the category dropdown.
                position.y += EditorGUIUtility.singleLineHeight;
                position.height = EditorGUIUtility.singleLineHeight;

                selectedCategoryIndex = EditorGUI.Popup(position, "Event Category",
                    selectedCategoryIndex, eventCategoriesKeyArray);

                if (selectedCategoryIndex != UNSELECTED)
                {
                    var eventsArray = eventCategories[eventCategoriesKeyArray[selectedCategoryIndex]].ToArray();

                    if (selectedEventIndex >= eventsArray.Length)
                        selectedEventIndex = 0;

                    // Create a new rectangle to position the events dropdown and Add button.
                    var selectedEventDropdownPosition = new Rect(position);

                    selectedEventDropdownPosition.y += EditorGUIUtility.singleLineHeight + 2;
                    selectedEventDropdownPosition.width = position.width - (BUTTON_WIDTH + (int)WitStyles.TextButtonPadding);

                    selectedEventIndex = EditorGUI.Popup(selectedEventDropdownPosition, "Event", selectedEventIndex,
                        eventsArray);

                    var selectedEventButtonPosition = new Rect(selectedEventDropdownPosition);

                    selectedEventButtonPosition.width = BUTTON_WIDTH;
                    selectedEventButtonPosition.x =
                        selectedEventDropdownPosition.x + selectedEventDropdownPosition.width + CONTROL_SPACING;

                    if (GUI.Button(selectedEventButtonPosition, "Add"))
                    {
                        var eventName = eventCategories[eventCategoriesKeyArray[selectedCategoryIndex]][
                            selectedEventIndex];

                        if (eventObject != null && selectedEventIndex != UNSELECTED &&
                            !eventObject.IsCallbackOverridden(eventName))
                        {
                            eventObject.RegisterOverriddenCallback(eventName);
                        }
                    }
                }

                // If any overrides have been added to the property, allow them to be edited
                if (eventObject != null && eventObject.OverriddenCallbacks.Count != 0)
                {
                    var propertyRect = new Rect(position.x, position.y + propertyOffset, position.width, 0);

                    SerializedProperty callbackProperty;

                    foreach (var callback in eventObject.OverriddenCallbacks)
                    {
                        callbackProperty = property.FindPropertyRelative(callback);

                        propertyRect.height = EditorGUI.GetPropertyHeight(callbackProperty, true);

                        EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative(callback));

                        propertyRect.y += propertyRect.height + CONTROL_SPACING;
                    }
                }

                EditorGUI.indentLevel--;
            }
        }
    }
}
