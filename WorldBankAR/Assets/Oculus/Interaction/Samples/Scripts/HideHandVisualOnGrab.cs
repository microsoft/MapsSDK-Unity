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

using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.Samples
{
    public class HideHandVisualOnGrab : MonoBehaviour
    {
        [SerializeField]
        private HandGrabInteractor _handGrabInteractor;

        [SerializeField, Interface(typeof(IHandVisual))]
        private UnityEngine.Object _handVisual;

        private IHandVisual HandVisual;

        protected virtual void Awake()
        {
            HandVisual = _handVisual as IHandVisual;
        }

        protected virtual void Start()
        {
            this.AssertField(HandVisual, nameof(HandVisual));
        }

        protected virtual void Update()
        {
            GameObject shouldHideHandComponent = null;

            if (_handGrabInteractor.State == InteractorState.Select)
            {
                shouldHideHandComponent = _handGrabInteractor.SelectedInteractable?.gameObject;
            }

            if (shouldHideHandComponent)
            {
                if (shouldHideHandComponent.TryGetComponent(out ShouldHideHandOnGrab component))
                {
                    HandVisual.ForceOffVisibility = true;
                }
            }
            else
            {
                HandVisual.ForceOffVisibility = false;
            }
        }

        #region Inject

        public void InjectAll(HandGrabInteractor handGrabInteractor,
             IHandVisual handVisual)
        {
            InjectHandGrabInteractor(handGrabInteractor);
            InjectHandVisual(handVisual);
        }
        private void InjectHandGrabInteractor(HandGrabInteractor handGrabInteractor)
        {
            _handGrabInteractor = handGrabInteractor;
        }

        private void InjectHandVisual(IHandVisual handVisual)
        {
            _handVisual = handVisual as UnityEngine.Object;
            HandVisual = handVisual;
        }


        #endregion
    }
}
