// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using Microsoft.Geospatial.VectorMath;
    using System;
    using UnityEngine;

    /// <summary>
    /// Animates a MapRenderer to the specified MapScene. Derives the animation duration and preforms a preceptually smooth animation,
    /// based on the work of van Wijk and Nuij, "Smooth and Efficient Zooming and Panning". https://www.win.tue.nl/~vanwijk/zoompan.pdf
    /// </summary>
    public class MapSceneAnimationController : IMapSceneAnimationController
    {
        private double _startZoomLevel;
        private double _endZoomLevel;
        private Vector2D _startMercatorCenter;
        private Vector2D _endMercatorCenter;
        private double _animationTime;
        private double _startHeightInMercator;
        private double _endHeightInMercator;
        private bool _isLinearAnimation;

        private float _runningTime;

        // The following are bow-animation specific fields as described in "Smooth and Efficient Zooming and Panning".
        const double P = 1.42; // Affects height of bow. Recommended by user study from paper.
        const double V = 0.75; // Affects the animation duration. Value results in slower animations compared to user study from paper, 0.9.
        private double _u0;
        private double _u1;
        private double _w0;
        private double _w1;
        private double _r0;
        private double _r1;
        private double _S;

        /// <inheritdoc/>
        public WaitForMapSceneAnimation YieldInstruction { get; } = new WaitForMapSceneAnimation();

        /// <inheritdoc/>
        public void Initialize(
            MapRenderer mapRenderer,
            MapScene mapScene,
            float animationTimeScale,
            MapSceneAnimationKind mapSceneAnimationKind)
        {
            _runningTime = 0;

            _startMercatorCenter = mapRenderer.Center.ToMercatorPosition();
            _startZoomLevel = mapRenderer.ZoomLevel;
            mapScene.GetLocationAndZoomLevel(out var endLocation, out _endZoomLevel);
            _endMercatorCenter = endLocation.ToMercatorPosition();
            _startHeightInMercator = ZoomLevelToMercatorAltitude(_startZoomLevel);
            _endHeightInMercator = ZoomLevelToMercatorAltitude(_endZoomLevel);

            // Determine if we should use a bow animation or a linear animation.
            // Simple rule for now: If the destination is already visible, use linear; otherwise, bow.
            _isLinearAnimation =
                mapSceneAnimationKind == MapSceneAnimationKind.Linear ||
                mapRenderer.Bounds.Intersects(endLocation);

            // While linear animation doesn't follow the same code path as the bow animation, we can still use this function to compute
            // a reasonable duration for the linear animation.
            ComputeBowAnimationInitialValues(
                P,
                V * animationTimeScale,
                out _u0,
                out _u1,
                out _w0,
                out _w1,
                out _r0,
                out _r1,
                out _S,
                out _animationTime);

            // Tweaking the resulting animation time to speed up slower animations and slow down the shorter animation.
            _animationTime = _animationTime / 6.0; // Normalize.
            _animationTime = Math.Pow(_animationTime, 1.0 / 3.0); // Rescale.
            _animationTime *= 6.0; // Convert back to original range.
        }

        /// <inheritdoc/>
        public bool UpdateAnimation(float currentZoomLevel, LatLon currentLocation, out float zoomLevel, out LatLon location)
        {
            _runningTime += Time.deltaTime;
            var t = Math.Min(Math.Max(0, _runningTime / _animationTime), 1);

            if (_isLinearAnimation)
            {
                // Ease t to slow down the start and stop of animations.
                t = (float)SmoothStep(0.0f, 1.0f, t);

                LinearAnimation(t, out zoomLevel, out location);
            }
            else
            {
                BowAnimation(t, currentZoomLevel, currentLocation, out zoomLevel, out location);
            }

            if (t >= 1)
            {
                YieldInstruction.SetComplete();
            }

            return t >= 1;
        }

        private void LinearAnimation(double t, out float zoomLevel, out LatLon location)
        {
            // First, update the zoom.
            zoomLevel = (float)(_startZoomLevel + (_endZoomLevel - _startZoomLevel) * t);

            // Update the location. 
            if (_startMercatorCenter != _endMercatorCenter)
            {
                if (_startZoomLevel != _endZoomLevel)
                {
                    // Adjust t so that it depends on the zoom level. This keeps the position animating correctly at a logarthmic scale to
                    // match how zoom level is being calculated.
                    var adjustedT = (_startHeightInMercator - ZoomLevelToMercatorAltitude(zoomLevel)) / (_startHeightInMercator - _endHeightInMercator);
                    var mercatorPosition = Interpolate(_startMercatorCenter, _endMercatorCenter, Math.Pow(adjustedT, 0.8));
                    location = new LatLon(mercatorPosition);
                }
                else
                {
                    var mercatorPosition = Interpolate(_startMercatorCenter, _endMercatorCenter, t);
                    location = new LatLon(mercatorPosition);
                }
            }
            else
            {
                location = new LatLon(_endMercatorCenter);
            }
        }

        /// <summary>
        /// Initializes values for the bow animation.
        /// </summary>
        private void ComputeBowAnimationInitialValues(
            double p,
            double v,
            out double u0,
            out double u1,
            out double w0,
            out double w1,
            out double r0,
            out double r1,
            out double S,
            out double animationTime)
        {
            u0 = 0;
            u1 = (_endMercatorCenter - _startMercatorCenter).Length();

            w0 = 1.0 / Math.Pow(2, _startZoomLevel - 1);
            w1 = 1.0 / Math.Pow(2, _endZoomLevel - 1);

            if (u0 == u1)
            {
                S = Math.Abs(Math.Log(w1 / w0)) / p;

                // r0 and r1 are unused.
                r0 = 0;
                r1 = 1;
            }
            else
            {
                r0 = R(0, w0, w1, u0, u1, p);
                r1 = R(1, w0, w1, u0, u1, p);
                S = (r1 - r0) / p;
            }

            animationTime = S / v;
        }

        /// <summary>
        /// Computes values for the bow animation given current progress through the animation.
        /// </summary>
        /// <remarks>
        /// This fails if u0 == u1, but in that case, the class should already be using linear animation and not bow animation.
        /// </remarks>
        private void BowAnimation(double t, float currentZoomLevel, LatLon currentLocation, out float zoomLevel, out LatLon location)
        {
            var s = Math.Max(t * _S, 0);

            // Calculate w (zoom) and u (position).

            var w = _w0 * Math.Cosh(_r0) / Math.Cosh(P * s + _r0);
            var u = (_w0 / (P * P)) * Math.Cosh(_r0) * Math.Tanh(P * s + _r0) - (_w0 / (P * P)) * Math.Sinh(_r0) + _u0;

            u = Math.Max(0, Math.Min(u / _u1, 1.0));

            zoomLevel = (float)MercatorAltitudeToZoomLevel(w);
            location = new LatLon(Vector2D.Lerp(_startMercatorCenter, _endMercatorCenter, u));
        }

        /// <summary>
        /// i = 0 or 1.
        /// </summary>
        private static double R(int i, double w0, double w1, double u0, double u1, double p)
        {
            var b_i = B(i, w0, w1, u0, u1, p);
            return Math.Log(-b_i + Math.Sqrt(b_i * b_i + 1));
        }

        /// <summary>
        /// i = 0 or 1.
        /// </summary>
        private static double B(int i, double w0, double w1, double u0, double u1, double p)
        {
            var uDiff = (u1 - u0);
            var numerator = w1 * w1 - w0 * w0 + Math.Pow(-1, i) * p * p * p * p * uDiff * uDiff;
            var denominator = 2 * (i == 0 ? w0 : w1) * p * p * uDiff;
            return numerator / denominator;
        }

        private static double ZoomLevelToMercatorAltitude(double zoomLevel)
        {
            return 1 / Math.Pow(2, zoomLevel - 1.0);
        }

        private static double MercatorAltitudeToZoomLevel(double mercatorAltitude)
        {
            return Math.Log(1.0 / mercatorAltitude, 2.0) + 1;
        }

        private static Vector2D Interpolate(Vector2D from, Vector2D to, double t)
        {
            return new Vector2D(
                from.X + (to.X - from.X) * t,
                from.Y + (to.Y - from.Y) * t);
        }

        private static double SmoothStep(double from, double to, double t)
        {
            t = -2.0 * t * t * t + 3.0 * t * t;
            return to * t + from * (1.0 - t);
        }
    }
}
