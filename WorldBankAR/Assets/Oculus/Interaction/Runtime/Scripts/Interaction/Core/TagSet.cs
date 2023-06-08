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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// A Tag Set that can be added to a GameObject with an Interactable to be filtered against.
    /// </summary>
    public class TagSet : MonoBehaviour
    {
        [SerializeField]
        private List<string> _tags;

        private HashSet<string> _tagSet;

        protected virtual void Start()
        {
            _tagSet = new HashSet<string>();
            foreach (string tag in _tags)
            {
                _tagSet.Add(tag);
            }
        }

        public bool ContainsTag(string tag) => _tagSet.Contains(tag);

        public void AddTag(string tag) => _tagSet.Add(tag);
        public void RemoveTag(string tag) => _tagSet.Remove(tag);

        #region Inject

        public void InjectOptionalTags(List<string> tags)
        {
            _tags = tags;
        }

        #endregion
    }
}
