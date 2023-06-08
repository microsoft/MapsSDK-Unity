/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Data.Info;

namespace Meta.WitAi
{
    /// <summary>
    /// Endpoint overrides
    /// </summary>
    [Serializable]
    public struct WitRequestEndpointOverride
    {
        public string uriScheme;
        public string authority;
        public string witApiVersion;
        public int port;
    }

    /// <summary>
    /// Configuration interface
    /// </summary>
    public interface IWitRequestConfiguration
    {
        string GetConfigurationId();
        string GetApplicationId();
        WitAppInfo GetApplicationInfo();
        WitRequestEndpointOverride GetEndpointOverrides();
        string GetClientAccessToken();
#if UNITY_EDITOR
        void SetClientAccessToken(string newToken);
        string GetServerAccessToken();
        void SetApplicationInfo(WitAppInfo appInfo);
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// A simple configuration for initial setup
    /// </summary>
    public class WitServerRequestConfiguration : IWitRequestConfiguration
    {
        private string _clientToken;
        private string _serverToken;

        public WitServerRequestConfiguration(string serverToken)
        {
            _serverToken = serverToken;
        }

        public string GetConfigurationId() => null;
        public string GetApplicationId() => null;
        public WitAppInfo GetApplicationInfo() => new WitAppInfo();

        public void SetApplicationInfo(WitAppInfo newInfo)
        {
        }

        public WitRequestEndpointOverride GetEndpointOverrides() => new WitRequestEndpointOverride();
        public string GetClientAccessToken() => _clientToken;
        public void SetClientAccessToken(string newToken) => _clientToken = newToken;
        public string GetServerAccessToken() => _serverToken;
    }
#endif
}
