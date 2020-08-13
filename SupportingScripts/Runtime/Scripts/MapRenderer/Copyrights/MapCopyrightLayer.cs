// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using System;
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// Handles rendering and positioning of copyright text associated with the <see cref="MapRendererBase"/>.
    /// This layer is automatically added when the <see cref="MapRendererBase"/> component is added to a <see cref="GameObject"/>.
    /// </summary>
    /// <remarks>
    /// If the default placement of the copyright text displayed by the <see cref="MapRendererBase"/> is unacceptable, it can be disabled
    /// and the text can be displayed manually by retrieving the copyright text string from <see cref="MapRendererBase.Copyright"/> and
    /// rendering it with a TextMesh or TextMeshPro component. The copyright text must be displayed in a conspicuous manner near the map.
    /// </remarks>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    [RequireComponent(typeof(MapRenderer))]
    public class MapCopyrightLayer : MapLayer
    {
        private const float Margin = 0.015f;

        /// <summary>
        /// The font used for the copyright text.
        /// </summary>
        [SerializeField]
        private Font _font = null;

        /// <summary>
        /// The color of the copyright text.
        /// </summary>
        [SerializeField]
        private Color _textColor = new Color(0, 0, 0, 0.42f);
        
        /// <summary>
        /// The alignment of the copyright text.
        /// </summary>
        [SerializeField]
        private MapCopyrightAlignment _mapCopyrightAlignment = MapCopyrightAlignment.Bottom;

        // Reload these fields lazily...
        // The corresponding text GameObjects will be marked HideAndDontSave, i.e. purely temporary.

        private TextMesh _copyrightText1;
        private TextMesh _copyrightText2;
        private Shader _occludable3DTextShader;

        private void Awake()
        {
            LayerName = "MapCopyrightLayer";
        }

        private void OnEnable()
        {
            MapRenderer.AfterUpdate -= UpdateDefaultCopyrights;
            MapRenderer.AfterUpdate += UpdateDefaultCopyrights;
            MapRenderer.AfterOnDisable -= MapRendererDisabled;
            MapRenderer.AfterOnDisable += MapRendererDisabled;

            if (_copyrightText1 != null)
            {
                _copyrightText1.gameObject.SetActive(true);
            }
            if (_copyrightText2 != null)
            {
                _copyrightText2.gameObject.SetActive(true);
            }
        }

        private void OnDisable()
        {
            MapRenderer.AfterUpdate -= UpdateDefaultCopyrights;
            MapRenderer.AfterOnDisable -= MapRendererDisabled;

            _copyrightText1?.gameObject.SetActive(false);
            _copyrightText2?.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_copyrightText1 != null)
            {
                DestroyImmediate(_copyrightText1.gameObject);
                _copyrightText1 = null;
            }

            if (_copyrightText2 != null)
            {
                DestroyImmediate(_copyrightText2.gameObject);
                _copyrightText2 = null;
            }
        }

        private void UpdateDefaultCopyrights(object sender, EventArgs args)
        {
            const int fontSize = 46;
            const float targetLocalSize = 0.175f;
            const float targetLocalScale = targetLocalSize / fontSize;
            var localYOffset = new Vector3(0, MapRenderer.LocalMapBaseHeight - 2 * targetLocalScale, 0);

            // Load the default font, if it hasn't already been loaded.
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                Debug.Assert(_font != null);
            }

            // Create the copyright game objects if they haven't already been created.
            EnsureCopyrightGameObjectSetup(ref _copyrightText1, "DefaultCopyright1", fontSize);
            EnsureCopyrightGameObjectSetup(ref _copyrightText2, "DefaultCopyright2", fontSize);

            // Sync the configurable properties.
            {
                var mapRendererGameObject = MapRenderer.gameObject;
                _copyrightText1.gameObject.layer = mapRendererGameObject.layer;
                _copyrightText1.text = MapRenderer.Copyright;
                _copyrightText1.color = _textColor;
                _copyrightText2.gameObject.layer = mapRendererGameObject.layer;
                _copyrightText2.text = MapRenderer.Copyright;
                _copyrightText2.color = _textColor;
            }

            // Update positions.
            {
                // First, determine which two sides of the map that we can see.
                var mainCamera = Camera.main;
                var cameraPosition = mainCamera == null ? Vector3.zero : mainCamera.transform.position;
                var thisTransform = transform;
                var cameraToPosition = thisTransform.position - cameraPosition;
                var transformForward = thisTransform.forward;
                var normal1Sign = Vector3.Dot(cameraToPosition, transformForward) > 0 ? -1.0f : 1.0f;
                var normal2Sign = Vector3.Dot(cameraToPosition, transform.right) > 0 ? -1.0f : 1.0f;
                var forward = transformForward * normal1Sign;
                var localForward = Vector3.forward * normal1Sign;
                var right = thisTransform.right * normal2Sign;
                var localRight = Vector3.right * normal2Sign;

                // Position the text meshes.
                UpdateTextPositionAndAlignment(
                    _copyrightText1,
                    MapRenderer.LocalMapDimension.y,
                    MapRenderer.LocalMapDimension.x,
                    new Vector3(Margin, 0, 0),
                    localForward,
                    localYOffset,
                    targetLocalScale);
                
                UpdateTextPositionAndAlignment(
                    _copyrightText2,
                    MapRenderer.LocalMapDimension.x,
                    MapRenderer.LocalMapDimension.y,
                    new Vector3(0, 0, Margin),
                    localRight,
                    localYOffset,
                    targetLocalScale);

                // Align the text meshes correctly.
                _copyrightText1.transform.rotation = Quaternion.LookRotation(-forward, transform.up);
                _copyrightText2.transform.rotation = Quaternion.LookRotation(-right, transform.up);
                
                // Enable / Disable the text depending on if the camera can see them.
                var cameraToText1Position = _copyrightText1.transform.position - cameraPosition;
                var cameraToText2Position = _copyrightText2.transform.position - cameraPosition;

                var isText1VisibleToCamera = Vector3.Dot(cameraToText1Position, -_copyrightText1.transform.forward) <= 0;
                var isText2VisibleToCamera = Vector3.Dot(cameraToText2Position, -_copyrightText2.transform.forward) <= 0;

                _copyrightText1.gameObject.SetActive(isText1VisibleToCamera);
                _copyrightText2.gameObject.SetActive(isText2VisibleToCamera);
            }
        }

        private void UpdateTextPositionAndAlignment(
            TextMesh textMesh,
            float localNormalMagnitude,
            float localCrossMagnitude,
            Vector3 marginLeft,
            Vector3 localNormal,
            Vector3 localYOffset,
            float localScale)
        {
            if (MapRenderer.MapShape == MapShape.Block)
            {
                var localCross = Vector3.Cross(localNormal, Vector3.up);
                textMesh.transform.localPosition =
                    0.504f * localNormalMagnitude * localNormal +
                    0.5f * localCrossMagnitude * -localCross +
                    (IsVectorNegative(localCross) ? -marginLeft : marginLeft);

                if (_mapCopyrightAlignment == MapCopyrightAlignment.Top)
                {
                    textMesh.anchor = TextAnchor.UpperLeft;
                    textMesh.transform.localPosition += localYOffset;
                }
                else
                {
                    textMesh.transform.localPosition += new Vector3(0, Margin, 0);
                    textMesh.anchor = TextAnchor.LowerLeft;
                }
                textMesh.alignment = TextAlignment.Left;
            }
            else
            {
                textMesh.transform.localPosition = MapRenderer.LocalMapRadius * localNormal;
                if (_mapCopyrightAlignment == MapCopyrightAlignment.Top)
                {
                    textMesh.anchor = TextAnchor.UpperCenter;
                    textMesh.transform.localPosition += localYOffset;
                }
                else
                {
                    textMesh.transform.localPosition += new Vector3(0, Margin, 0);
                    textMesh.anchor = TextAnchor.LowerCenter;
                }
                textMesh.alignment = TextAlignment.Center;
            }
            textMesh.transform.localScale = new Vector3(localScale, localScale, localScale);
        }

        private static bool IsVectorNegative(Vector3 v)
        {
            return v.x < 0 || v.z < 0;
        }

        private void EnsureCopyrightGameObjectSetup(ref TextMesh textMesh, string copyrightGameObjectName, int fontSize)
        {
            if (textMesh == null)
            {
                var defaultCopyrightText =
                    new GameObject(copyrightGameObjectName)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                defaultCopyrightText.transform.parent = transform;

                textMesh = defaultCopyrightText.AddComponent<TextMesh>();
                textMesh.font = _font;
                textMesh.fontSize = fontSize;
                textMesh.fontStyle = UnityEngine.FontStyle.Bold;

                if (_occludable3DTextShader == null)
                {
                    _occludable3DTextShader = Shader.Find("MapsSDK/Occludable3DTextShader");
                }

                if (_occludable3DTextShader != null)
                {
                    var meshRenderer = textMesh.GetComponent<MeshRenderer>();
                    meshRenderer.sharedMaterial = new Material(_occludable3DTextShader);
                    meshRenderer.receiveShadows = true;
                    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    if (textMesh.font != null)
                    {
                        meshRenderer.sharedMaterial.SetTexture("_MainTex", textMesh.font.material.mainTexture);
                    }
                }
            }
            else
            {
                textMesh.gameObject.SetActive(true);
            }
        }

        private void MapRendererDisabled(object sender, EventArgs args)
        {
            _copyrightText1?.gameObject.SetActive(false);
            _copyrightText2?.gameObject.SetActive(false);
        }
    }
}
