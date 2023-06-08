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

namespace Oculus.Interaction
{
    public static class BoundsExtensions
    {
        public static bool Clip(this Bounds bounds,
            in Bounds clipper, out Bounds result)
        {
            result = new Bounds();
            Vector3 min = Vector3.Max(bounds.min, clipper.min);
            Vector3 max = Vector3.Min(bounds.max, clipper.max);

            if (min.x > max.x ||
                min.y > max.y ||
                min.z > max.z)
            {
                return false;
            }

            result.SetMinMax(min, max);
            return true;
        }
    }
}
