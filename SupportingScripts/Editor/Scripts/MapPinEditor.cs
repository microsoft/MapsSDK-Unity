// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using System;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(MapPin))]
    [CanEditMultipleObjects]
    internal class MapPinEditor : Editor
    {
        private SerializedProperty _locationProperty;
        private SerializedProperty _altitude;
        private SerializedProperty _altitudeReferenceProperty;
        private SerializedProperty _useRealworldScaleProperty;
        private SerializedProperty _scaleCurveProperty;
        private SerializedProperty _isLayerSynchronizedProperty;
        private SerializedProperty _showOutsideMapBoundsProperty;

        private Vector3 _mouseDownMapPinPlanePositionInMapLocalSpace;
        private MercatorCoordinate _mouseDownMapPinPositionInMercatorSpace;
        private bool _isHovered;
        private bool _isDragging;

        private void OnEnable()
        {
            _locationProperty = serializedObject.FindProperty("_location");
            _altitude = serializedObject.FindProperty("_altitude");
            _altitudeReferenceProperty = serializedObject.FindProperty("_altitudeReference");
            _useRealworldScaleProperty = serializedObject.FindProperty("_useRealWorldScale");
            _scaleCurveProperty = serializedObject.FindProperty("_scaleCurve");
            _isLayerSynchronizedProperty = serializedObject.FindProperty("_isLayerSynchronized");
            _showOutsideMapBoundsProperty = serializedObject.FindProperty("_showOutsideMapBounds");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(_locationProperty, true);
            EditorGUILayout.PropertyField(_altitude, new GUIContent("Altitude (meters)"));
            EditorGUILayout.PropertyField(_altitudeReferenceProperty);
            EditorGUILayout.PropertyField(_useRealworldScaleProperty);
            EditorGUILayout.PropertyField(_scaleCurveProperty);
            EditorGUILayout.PropertyField(_isLayerSynchronizedProperty);
            EditorGUILayout.PropertyField(_showOutsideMapBoundsProperty);

            // If childed to a MapRenderer, don't show the transform component since the MapRenderer is constantly overriding it.
            var mapPin = (MapPin)target;
            if (mapPin.transform.parent != null && mapPin.transform.parent.GetComponent<MapRenderer>() != null)
            {
                mapPin.transform.hideFlags = HideFlags.NotEditable;
            }
            else
            {
                mapPin.transform.hideFlags = HideFlags.None;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            // If childed to a MapRenderer, don't use the default tools. Use a tool that can update the MapPin's location.
            var mapPin = (MapPin)target;
            var mapRenderer = mapPin.transform.parent?.GetComponent<MapRenderer>();
            if (mapRenderer != null)
            {
                var transform = mapPin.transform;

                Tools.hidden = true;

                var controlId = GUIUtility.GetControlID("MapPinEditorHandle".GetHashCode(), FocusType.Passive);
                var size = 0.025f;

                switch (Event.current.GetTypeForControl(controlId))
                {
                    case EventType.Layout:
                        Handles.RectangleHandleCap(
                            controlId,
                            _isDragging ? new Vector3(transform.position.x, (float)_mouseDownMapPinPositionInMercatorSpace.Y, transform.position.z) : transform.position,
                            transform.rotation * Quaternion.LookRotation(Vector3.up),
                            size,
                            EventType.Layout);
                        break;
                    case EventType.MouseMove:
                        if (HandleUtility.nearestControl == controlId)
                        {
                            _isHovered = true;
                            Event.current.Use();
                        }
                        else
                        {
                            _isHovered = false;
                        }
                        break;
                    case EventType.MouseDown:
                        if (HandleUtility.nearestControl == controlId)
                        {
                            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                            var plane = new Plane(transform.up, transform.position);
                            if (plane.Raycast(ray, out var enter))
                            {
                                // Respond to a press on this handle. Drag starts automatically.
                                _mouseDownMapPinPlanePositionInMapLocalSpace = mapRenderer.transform.worldToLocalMatrix * ray.GetPoint(enter);
                                _mouseDownMapPinPositionInMercatorSpace = mapPin.Location.ToMercatorCoordinate();
                                _isDragging = true;
                                GUIUtility.hotControl = controlId;
                                Event.current.Use();
                            }
                        }
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == controlId)
                        {
                            // Respond to a release on this handle. Drag stops automatically.
                            _isDragging = false;
                            GUIUtility.hotControl = 0;
                            Event.current.Use();
                        }
                        break;

                    case EventType.MouseDrag:
                        if (_isDragging)
                        {
                            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                            var plane = new Plane(transform.up, _mouseDownMapPinPlanePositionInMapLocalSpace);
                            if (plane.Raycast(ray, out var enter))
                            {
                                Vector3 updatedHitPointInLocalSpace = mapRenderer.transform.worldToLocalMatrix * ray.GetPoint(enter);
                                var newDeltaInLocalSpace = updatedHitPointInLocalSpace - _mouseDownMapPinPlanePositionInMapLocalSpace;
                                var newDeltaInMercator = new MercatorCoordinate(newDeltaInLocalSpace.x, newDeltaInLocalSpace.z) / Math.Pow(2, mapRenderer.ZoomLevel - 1);
                                var newLocation = (_mouseDownMapPinPositionInMercatorSpace + newDeltaInMercator).ToLatLon();

                                Undo.RecordObject(target, "Changed Location");
                                mapPin.Location = newLocation;
                            }
                            Event.current.Use();
                        }
                        break;
                    case EventType.Repaint:
                        if (_isHovered || _isDragging)
                        {
                            // Change the cursor to make it obvious you can move the MapPin around.
                            EditorGUIUtility.AddCursorRect(new Rect(0, 0, 999999, 999999), MouseCursor.MoveArrow);
                        }

                        var planeY = _isDragging ? (mapRenderer.transform.localToWorldMatrix * _mouseDownMapPinPlanePositionInMapLocalSpace).y : transform.position.y;
                        var rectanglePosition = new Vector3(transform.position.x, planeY, transform.position.z);
                        Handles.DrawSolidRectangleWithOutline(
                            new Vector3[]
                            {
                                    rectanglePosition - size * mapRenderer.transform.right - size * mapRenderer.transform.forward,
                                    rectanglePosition - size * mapRenderer.transform.right + size * mapRenderer.transform.forward,
                                    rectanglePosition + size * mapRenderer.transform.right + size * mapRenderer.transform.forward,
                                    rectanglePosition + size * mapRenderer.transform.right - size * mapRenderer.transform.forward
                            },
                            new Color(0.4f, 0.4f, 0.4f, 0.4f),
                            new Color(0, 0, 0, 0));

                        if (rectanglePosition != transform.position)
                        {
                            Handles.color = rectanglePosition.y > transform.position.y ? Color.red : Color.green;
                            Handles.DrawLine(transform.position, rectanglePosition);
                        }

                        Handles.color = _isHovered ? Color.yellow : Color.green;
                        Handles.color = _isDragging ? Color.white : Handles.color;
                        Handles.RectangleHandleCap(
                            controlId,
                            rectanglePosition,
                            transform.rotation * Quaternion.LookRotation(Vector3.up),
                            size,
                            EventType.Repaint);
                        break;
                }
            }
            else
            {
                Tools.hidden = false;
            }
        }
    }
}
