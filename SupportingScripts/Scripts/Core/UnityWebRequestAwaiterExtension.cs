// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEngine.Networking;

    internal static class UnityWebRequestAwaiterExtension
    {
        public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation webRequestAsyncOperation)
        {
            return new UnityWebRequestAwaiter(webRequestAsyncOperation);
        }
    }
}
