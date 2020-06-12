// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using System;
    using UnityEngine;

    /// <summary>
    /// This component provides an interface to configure the cache size that is used to store <see cref="MapRenderer"/> data.
    /// Because the cache is global, multiple instances of this component are not needed.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class MapDataCache : MapDataCacheBase
    {
        private const long DefaultMaxCacheSizeInBytes = 2147483648L; // 2 GB
        private const long MaxMaxCacheSizeInBytes = 4294967296L; // 4 GB
        private const long MinCacheSizeInBytes = 209715200L; // 200 MB

        /// <summary>
        /// Determines how large the cache is relative to the estimated application memory limit, e.g. if the maximum app memory for the
        /// application is determined to be 1GB, then a utilization value of 0.5 (50%), will result in a cache size of 512MB.
        /// </summary>
        [SerializeField]
        [Range(0, 1)]
        private float _percentUtilization = 0.3f;

        /// <summary>
        /// Determines how large the cache is relative to the estimated application memory limit, e.g. if the maximum app memory for the
        /// application is determined to be 1GB, then a utilization value of 0.5 (50%), will result in a cache size of 512MB.
        /// </summary>
        public float PercentUtilization
        {
            get => _percentUtilization;
            set
            {
                var newPercentUtilization = Mathf.Clamp01(_percentUtilization);
                if (newPercentUtilization != _percentUtilization)
                {
                    _percentUtilization = newPercentUtilization;
                    Refresh();
                }
            }
        }

        /// <summary>
        /// The maximum possible cache size. Even if the device could support a larger cache, the cache size will not exceed this value.
        /// </summary>
        [SerializeField]
        private long _maxCacheSizeInBytes = DefaultMaxCacheSizeInBytes;

        /// <summary>
        /// The maximum possible cache size. Even if the device could support a larger cache, the cache size will not exceed this value.
        /// </summary>
        public long MaxCacheSizeInBytes
        {
            get => _maxCacheSizeInBytes;
            set
            {
                var newMaxCacheSizeInBytes = Math.Min(MaxMaxCacheSizeInBytes, _maxCacheSizeInBytes);
                if (_maxCacheSizeInBytes != newMaxCacheSizeInBytes)
                {
                    _maxCacheSizeInBytes = newMaxCacheSizeInBytes;
                    Refresh();
                }
            }
        }

        private void OnValidate()
        {
            _maxCacheSizeInBytes = Math.Min(MaxMaxCacheSizeInBytes, _maxCacheSizeInBytes);
            _percentUtilization = Mathf.Clamp01(_percentUtilization);
            Refresh();
        }

        /// <inheritdoc/>
        protected override long ComputeCacheSizeInBytes()
        {
            var maximumAvailableBytes = 0L;

#if UNITY_ANDROID
            // On Android, we can query the application's memory limit to get an accurate max size.
            if (!Application.isEditor)
            {
                try
                {
                    maximumAvailableBytes = GetAvailableMemory();
                    maximumAvailableBytes = (long)(maximumAvailableBytes * 0.9f); // Leave some room.
                }
                catch (Exception e)
                {
                    Debug.LogException(e, gameObject);
                }
            }
#endif
            // The fall back option is to just compute a maximum size based on the total available memory.
            if (maximumAvailableBytes <= 0)
            {
                var totalSystemMemoryInBytes = SystemInfo.systemMemorySize /* <-- MB */ * 1000L * 1000L;
                var managedBytes = GC.GetTotalMemory(true);
                maximumAvailableBytes = totalSystemMemoryInBytes - managedBytes;
            }

            var percentUtilization = Mathf.Clamp01(_percentUtilization);
            var cacheSizeInBytes = (long)(percentUtilization * maximumAvailableBytes);
            return Math.Max(MinCacheSizeInBytes, Math.Min(cacheSizeInBytes, _maxCacheSizeInBytes));
        }

#if UNITY_ANDROID

        private static AndroidJavaObject GetMemoryInfo()
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var systemService = currentActivity.Call<AndroidJavaObject>("getSystemService", "activity");
            var memoryInfo = new AndroidJavaObject("android.app.ActivityManager$MemoryInfo");
            systemService.Call("getMemoryInfo", memoryInfo);
            return memoryInfo;
        }
 
        private static long GetAvailableMemory()
        {
            using (var memoryInfo = GetMemoryInfo())
            {
                return memoryInfo.Get<long>("availMem");
            }
        }

#endif
    }
}
