/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;

namespace Oculus.Interaction.Surfaces
{
    public class PlaneSurface : MonoBehaviour, ISurface, IBounds
    {
        public enum NormalFacing
        {
            /// <summary>
            /// Normal faces along the transform's negative Z axis
            /// </summary>
            Backward,

            /// <summary>
            /// Normal faces along the transform's positive Z axis
            /// </summary>
            Forward,
        }

        [Tooltip("The normal facing of the surface. Hits will be " +
            "registered either on the front or back of the plane " +
            "depending on this value.")]
        [SerializeField]
        private NormalFacing _facing = NormalFacing.Backward;

        [SerializeField, Tooltip("Raycasts hit either side of plane, but hit normal " +
        "will still respect plane facing.")]
        private bool _doubleSided = false;

        public NormalFacing Facing
        {
            get => _facing;
            set => _facing = value;
        }

        public bool DoubleSided
        {
            get => _doubleSided;
            set => _doubleSided = value;
        }

        public Vector3 Normal
        {
            get
            {
                return _facing == NormalFacing.Forward ?
                                  transform.forward :
                                  -transform.forward;
            }
        }

        private bool IsPointAboveSurface(Vector3 point)
        {
            Plane plane = GetPlane();
            return plane.GetSide(point);
        }

        public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance)
        {
            hit = new SurfaceHit();
            Plane plane = GetPlane();

            float hitDistance = plane.GetDistanceToPoint(point);
            if (maxDistance > 0 && Mathf.Abs(hitDistance) > maxDistance)
            {
                return false;
            }

            hit.Point = plane.ClosestPointOnPlane(point);
            hit.Distance = IsPointAboveSurface(point) ? hitDistance : -hitDistance;
            hit.Normal = plane.normal;

            return true;
        }

        public Transform Transform => transform;

        public Bounds Bounds
        {
            get
            {
                Vector3 size = new Vector3(
                    Mathf.Abs(Normal.x) == 1f ? float.Epsilon : float.PositiveInfinity,
                    Mathf.Abs(Normal.y) == 1f ? float.Epsilon : float.PositiveInfinity,
                    Mathf.Abs(Normal.z) == 1f ? float.Epsilon : float.PositiveInfinity);
                return new Bounds(transform.position, size);
            }
        }

        public bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance)
        {
            hit = new SurfaceHit();
            Plane plane = GetPlane();

            if (!_doubleSided && !IsPointAboveSurface(ray.origin))
            {
                return false;
            }

            if (plane.Raycast(ray, out float hitDistance))
            {
                if (maxDistance > 0 && hitDistance > maxDistance)
                {
                    return false;
                }

                hit.Point = ray.GetPoint(hitDistance);
                hit.Normal = plane.normal;
                hit.Distance = hitDistance;
                return true;
            }

            return false;
        }

        public Plane GetPlane()
        {
            return new Plane(Normal, transform.position);
        }

        #region Inject

        public void InjectAllPlaneSurface(NormalFacing facing,
            bool doubleSided)
        {
            InjectNormalFacing(facing);
            InjectDoubleSided(doubleSided);
        }

        public void InjectNormalFacing(NormalFacing facing)
        {
            _facing = facing;
        }

        public void InjectDoubleSided(bool doubleSided)
        {
            _doubleSided = doubleSided;
        }

        #endregion
    }
}
