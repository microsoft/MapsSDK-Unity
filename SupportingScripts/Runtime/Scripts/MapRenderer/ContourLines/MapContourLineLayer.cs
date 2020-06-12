// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Enables contour rendering on the map to show lines of constant elevation relative to the WGS84 ellipsoid.
    /// The interval, line width, and line color of the contour is configurable.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class MapContourLineLayer : MapLayer
    {
        /// <summary>
        /// The interval between major contour lines.
        /// </summary>
        [SerializeField]
        [Range(0.01f, 10)]
        private float _majorIntervalAltitudeInMeters = 2;

        /// <summary>
        /// The altitude interval between major contour lines. This will automatically adjust
        /// to the current zoom level of the MapRenderer. This value specifies the interval used
        /// at zoom level 20.
        /// </summary>
        public float MajorIntervalAltitudeInMeters
        {
            get => _majorIntervalAltitudeInMeters;
            set => _majorIntervalAltitudeInMeters = Mathf.Clamp(value, 0.01f, 10.0f);
        }

        /// <summary>
        /// The number of minor sections between major contour lines.
        /// </summary>
        [SerializeField]
        [Range(1, 50)]
        private int _numMinorIntervalSections = 5;

        /// <summary>
        /// The number of minor sections between major contour lines.
        /// </summary>
        public int NumMinorIntervalSections
        {
            get => _numMinorIntervalSections;
            set => _numMinorContourIntervalSections = Mathf.Clamp(value, 1, 50);
        }

        /// <summary>
        /// The color used for the major contour lines.
        /// </summary>
        [SerializeField]
        private Color _majorColor = Color.white;

        /// <summary>
        /// The color used for the major contour lines.
        /// </summary>
        public Color MajorColor
        {
            get => _majorColor;
            set => _majorColor = value;
        }

        /// <summary>
        /// The color used for the minor contour lines.
        /// </summary>
        [SerializeField]
        private Color _minorColor = Color.gray;

        /// <summary>
        /// The color used for the minor contour lines.
        /// </summary>
        public Color MinorColor
        {
            get => _minorColor;
            set => _minorColor = value;
        }

        /// <summary>
        /// The pixel size of the major contour lines.
        /// </summary>
        [SerializeField]
        [Range(1.0f, 3.0f)]
        private float _majorLinePixelSize = 1.5f;

        /// <summary>
        /// The pixel size of the major contour lines.
        /// </summary>
        public float MajorLinePixelSize
        {
            get => _majorLinePixelSize;
            set => _majorLinePixelSize = Mathf.Clamp(value, 1.0f, 3.0f);
        }

        /// <summary>
        /// The pixel size of the minor contour lines.
        /// </summary>
        [SerializeField]
        [Range(1.0f, 3.0f)]
        private float _minorLinePixelSize = 1.0f;

        /// <summary>
        /// The pixel size of the minor contour lines.
        /// </summary>
        public float MinorLinePixelSize
        {
            get => _minorLinePixelSize;
            set => _minorLinePixelSize = Mathf.Clamp(value, 1.0f, 3.0f);
        }

        private int _majorContourLineColorShaderId;
        private int _minorContourLineColorShaderId;
        private int _halfMajorContourLinePixelSizeShaderId;
        private int _halfMinorContourLinePixelSizeShaderId;
        private int _numMinorContourIntervalSections;
        private int _minorContourLineIntervalInMetersShaderId;

        private void Awake()
        {
            LayerName = "MapContourLineLayer";
        }

        private void OnEnable()
        {
            MapRenderer.EnableMaterialKeyword("ENABLE_CONTOUR_LINES");
        }

        private void Start()
        {
            _majorContourLineColorShaderId = Shader.PropertyToID("_MajorContourLineColor");
            _minorContourLineColorShaderId = Shader.PropertyToID("_MinorContourLineColor");
            _halfMajorContourLinePixelSizeShaderId = Shader.PropertyToID("_HalfMajorContourLinePixelSize");
            _halfMinorContourLinePixelSizeShaderId = Shader.PropertyToID("_HalfMinorContourLinePixelSize");
            _numMinorContourIntervalSections = Shader.PropertyToID("_NumMinorContourIntervalSections");
            _minorContourLineIntervalInMetersShaderId = Shader.PropertyToID("_MinorContourLineIntervalInMeters");
        }

        private void Update()
        {
            Shader.SetGlobalColor(
                _majorContourLineColorShaderId,
                new Color(_majorColor.r * _majorColor.a, _majorColor.g * _majorColor.a, _majorColor.b * _majorColor.a, _majorColor.a));
            Shader.SetGlobalColor(
                _minorContourLineColorShaderId,
                new Color(_minorColor.r * _minorColor.a, _minorColor.g * _minorColor.a, _minorColor.b * _minorColor.a, _minorColor.a));
            Shader.SetGlobalFloat(_halfMajorContourLinePixelSizeShaderId, 0.5f * _majorLinePixelSize);
            Shader.SetGlobalFloat(_halfMinorContourLinePixelSizeShaderId, 0.5f * _minorLinePixelSize);
            Shader.SetGlobalFloat(_numMinorContourIntervalSections, _numMinorIntervalSections);

            var majorIntervalAltitudeInMeters = (float)(_majorIntervalAltitudeInMeters * Math.Pow(2, 20 - Mathf.RoundToInt(MapRenderer.ZoomLevel)));
            Shader.SetGlobalFloat(_minorContourLineIntervalInMetersShaderId, majorIntervalAltitudeInMeters / _numMinorIntervalSections);
        }

        private void OnDisable()
        {
            MapRenderer.DisableMaterialKeyword("ENABLE_CONTOUR_LINES");
        }
    }
}
