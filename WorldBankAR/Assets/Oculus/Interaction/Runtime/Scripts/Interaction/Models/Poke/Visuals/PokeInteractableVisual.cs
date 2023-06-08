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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    public class PokeInteractableVisual : MonoBehaviour
    {
        [SerializeField]
        private PokeInteractable _pokeInteractable;

        [SerializeField]
        private Transform _buttonBaseTransform;

        private float _maxOffsetAlongNormal;
        private Vector2 _planarOffset;

        private HashSet<PokeInteractor> _pokeInteractors;
        private PokeInteractor _postProcessInteractor;

        private Action _postProcessHandler => UpdateComponentPosition;


        protected bool _started = false;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_pokeInteractable, nameof(_pokeInteractable));
            this.AssertField(_buttonBaseTransform, nameof(_buttonBaseTransform));
            _pokeInteractors = new HashSet<PokeInteractor>();
            _maxOffsetAlongNormal = Vector3.Dot(transform.position - _buttonBaseTransform.position, -1f * _buttonBaseTransform.forward);
            Vector3 pointOnPlane = transform.position - _maxOffsetAlongNormal * _buttonBaseTransform.forward;
            _planarOffset = new Vector2(
                                Vector3.Dot(pointOnPlane - _buttonBaseTransform.position, _buttonBaseTransform.right),
                Vector3.Dot(pointOnPlane - _buttonBaseTransform.position, _buttonBaseTransform.up));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _pokeInteractors.Clear();
                _pokeInteractors.UnionWith(_pokeInteractable.Interactors);
                _pokeInteractable.WhenInteractorAdded.Action += HandleInteractorAdded;
                _pokeInteractable.WhenInteractorRemoved.Action += HandleInteractorRemoved;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _pokeInteractors.Clear();
                _pokeInteractable.WhenInteractorAdded.Action -= HandleInteractorAdded;
                _pokeInteractable.WhenInteractorRemoved.Action -= HandleInteractorRemoved;

                if (_postProcessInteractor)
                {
                    _postProcessInteractor.WhenPostprocessed -= _postProcessHandler;
                    _postProcessInteractor = null;
                }
            }
        }

        private void HandleInteractorAdded(PokeInteractor pokeInteractor)
        {
            _pokeInteractors.Add(pokeInteractor);

            if (_postProcessInteractor == null)
            {
                _postProcessInteractor = pokeInteractor;
                _postProcessInteractor.WhenPostprocessed += _postProcessHandler;
            }
        }

        private void HandleInteractorRemoved(PokeInteractor pokeInteractor)
        {
            _pokeInteractors.Remove(pokeInteractor);

            if (pokeInteractor == _postProcessInteractor)
            {
                _postProcessInteractor.WhenPostprocessed -= _postProcessHandler;

                // Subscribe to any remaining poke interactor that is hovering. It doesn't really
                // matter which, so take the first in the unordered enumeration of the hashset.
                using var enumerator = _pokeInteractors.GetEnumerator();
                if (enumerator.MoveNext() && enumerator.Current != null)
                {
                    _postProcessInteractor = enumerator.Current;
                    _postProcessInteractor.WhenPostprocessed += _postProcessHandler;
                }
                else
                {
                    _postProcessInteractor = null;
                    
                    // There are no interactors in hover state. Update component position one last
                    // time to put it at the max offset.
                    UpdateComponentPosition();
                }
            }
        }

        private void UpdateComponentPosition()
        {
            // To create a pressy button visual, we check each near poke interactor's
            // depth against the base of the button and use the most pressed-in
            // value as our depth. We cap this at the button base as the stopping
            // point. If no interactors exist, we sit the button at the original offset

            float closestDistance = _maxOffsetAlongNormal;
            foreach (PokeInteractor pokeInteractor in _pokeInteractors)
            {
                // Scalar project the poke interactor's position onto the button base's normal vector
                float pokeDistance =
                    Vector3.Dot(pokeInteractor.Origin - _buttonBaseTransform.position,
                        -1f * _buttonBaseTransform.forward);
                pokeDistance -= pokeInteractor.Radius;
                if (pokeDistance < 0f)
                {
                    pokeDistance = 0f;
                }

                closestDistance = Math.Min(pokeDistance, closestDistance);
            }

            // Position our transformation at our button base plus
            // the most pressed in distance along the normal plus
            // the original planar offset of the button from the button base
            transform.position = _buttonBaseTransform.position +
                                 _buttonBaseTransform.forward * (-1f * closestDistance) +
                                 _buttonBaseTransform.right * _planarOffset.x +
                                 _buttonBaseTransform.up * _planarOffset.y;
        }

        #region Inject

        public void InjectAllPokeInteractableVisual(PokeInteractable pokeInteractable,
            Transform buttonBaseTransform)
        {
            InjectPokeInteractable(pokeInteractable);
            InjectButtonBaseTransform(buttonBaseTransform);
        }

        public void InjectPokeInteractable(PokeInteractable pokeInteractable)
        {
            _pokeInteractable = pokeInteractable;
        }

        public void InjectButtonBaseTransform(Transform buttonBaseTransform)
        {
            _buttonBaseTransform = buttonBaseTransform;
        }

        #endregion
    }
}
