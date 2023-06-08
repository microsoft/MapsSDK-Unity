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

using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    public class ControllerOffset : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IController))]
        private UnityEngine.Object _controller;
        public IController Controller { get; private set; }

        [SerializeField]
        private Vector3 _offset;

        [SerializeField]
        private Quaternion _rotation = Quaternion.identity;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Controller = _controller as IController;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Controller, nameof(Controller));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Controller.WhenUpdated += HandleUpdated;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Controller.WhenUpdated -= HandleUpdated;
            }
        }

        private void HandleUpdated()
        {
            if (Controller.TryGetPose(out Pose rootPose))
            {
                Pose pose = new Pose(Controller.Scale * _offset, _rotation);
                pose.Postmultiply(rootPose);
                transform.SetPose(pose);
            }
        }

        public void GetOffset(ref Pose pose)
        {
            pose.position = Controller.Scale * _offset;
            pose.rotation = _rotation;
        }

        public void GetWorldPose(ref Pose pose)
        {
            pose.position = this.transform.position;
            pose.rotation = this.transform.rotation;
        }

        #region Inject

        public void InjectController(IController controller)
        {
            _controller = controller as UnityEngine.Object;
            Controller = controller;
        }

        public void InjectOffset(Vector3 offset)
        {
            _offset = offset;
        }
        public void InjectRotation(Quaternion rotation)
        {
            _rotation = rotation;
        }

        public void InjectAllControllerOffset(IController controller,
            Vector3 offset, Quaternion rotation)
        {
            InjectController(controller);
            InjectOffset(offset);
            InjectRotation(rotation);
        }
        #endregion
    }
}
