// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEngine.Networking;

    /// <summary>
    /// Provides the ability to use await keyword with <see cref="UnityWebRequestAsyncOperation"/>.
    /// </summary>
    public static class UnityWebRequestAwaiterExtensionMethods
    {
        /// <summary>
        /// Provides the ability to use await keyword with <see cref="UnityWebRequestAsyncOperation"/>.
        /// </summary>
        public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation webRequestAsyncOperation)
        {
            return new UnityWebRequestAwaiter(webRequestAsyncOperation);
        }
    }
}
