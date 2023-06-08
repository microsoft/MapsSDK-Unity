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
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// Associate a primary interactor to this GameObject
    /// </summary>
    public class SecondaryInteractorConnection : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IInteractorView))]
        private UnityEngine.Object _primaryInteractor;
        [SerializeField, Interface(typeof(IInteractorView))]
        private UnityEngine.Object _secondaryInteractor;
        public IInteractorView PrimaryInteractor { get; private set; }
        public IInteractorView SecondaryInteractor { get; private set; }

        protected virtual void Awake()
        {
            PrimaryInteractor = _primaryInteractor as IInteractorView;
            SecondaryInteractor = _secondaryInteractor as IInteractorView;
        }

        protected virtual void Start()
        {
            this.AssertField(PrimaryInteractor, nameof(PrimaryInteractor));
            this.AssertField(SecondaryInteractor, nameof(SecondaryInteractor));
        }

        #region Inject

        public void InjectAllSecondaryInteractorConnection(
            IInteractorView primaryInteractor,
            IInteractorView secondaryInteractor)
        {
            InjectPrimaryInteractor(primaryInteractor);
            InjectSecondaryInteractorConnection(secondaryInteractor);
        }
        public void InjectPrimaryInteractor(IInteractorView interactorView)
        {
            PrimaryInteractor = interactorView;
            _primaryInteractor = interactorView as UnityEngine.Object;
        }

        public void InjectSecondaryInteractorConnection(IInteractorView interactorView)
        {
            SecondaryInteractor = interactorView;
            _secondaryInteractor = interactorView as UnityEngine.Object;
        }

        #endregion
    }
}
