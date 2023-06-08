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

namespace Oculus.Interaction.DistanceReticles
{
    public class ReticleDataIcon : MonoBehaviour, IReticleData
    {
        [SerializeField, Optional]
        private MeshRenderer _renderer;

        [SerializeField, Optional]
        private Texture _customIcon;
        public Texture CustomIcon
        {
            get
            {
                return _customIcon;
            }
            set
            {
                _customIcon = value;
            }
        }

        [SerializeField]
        [Range(0f, 1f)]
        private float _snappiness;
        public float Snappiness
        {
            get
            {
                return _snappiness;
            }
            set
            {
                _snappiness = value;
            }
        }

        public Vector3 GetTargetSize()
        {
            if (_renderer != null)
            {
                return _renderer.bounds.size;
            }
            return this.transform.localScale;
        }

        public Vector3 ProcessHitPoint(Vector3 hitPoint)
        {
            return Vector3.Lerp(hitPoint, this.transform.position, _snappiness);
        }

        private Vector3 NearestColliderHit(Ray ray, Collider collider, out float score)
        {
            Vector3 centerPosition = collider.bounds.center;
            Vector3 projectedCenter = ray.origin
                + Vector3.Project(centerPosition - ray.origin, ray.direction);
            Vector3 point = collider.ClosestPointOnBounds(projectedCenter);
            Vector3 originToInteractable = point - ray.origin;
            score = Vector3.Angle(originToInteractable.normalized, ray.direction);

            return point;
        }

        #region Inject
        public void InjectOptionalRenderer(MeshRenderer renderer)
        {
            _renderer = renderer;
        }

        #endregion
    }
}
