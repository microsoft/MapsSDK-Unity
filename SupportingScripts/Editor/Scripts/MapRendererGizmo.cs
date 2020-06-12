// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Custom gizmo which makes a MapRenderer selectable in the editor.
    /// </summary>
    public static class MapRendererGizmo
    {
        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Pickable | GizmoType.Selected)]
        static void Gizmo(MapRenderer mapRenderer, GizmoType type)
        {
            // Draw a transparent cube that encompasses the base of the map. It is selectable in the editor, allowing the map to be selected,
            // but because it is completely transparent, it won't be visible.
            var color = new Color(0, 0, 0, 0);
            Gizmos.matrix = mapRenderer.gameObject.transform.localToWorldMatrix;
            Gizmos.color = color;
            Gizmos.DrawCube(
                new Vector3(0, mapRenderer.LocalMapBaseHeight / 2, 0),
                new Vector3(mapRenderer.LocalMapDimension.x, mapRenderer.LocalMapBaseHeight, mapRenderer.LocalMapDimension.y));
        }
    }
}
