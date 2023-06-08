/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi;
using Meta.WitAi.Data.Configuration;

namespace Meta.WitAi.Configuration
{
    [Serializable]
    public class WitEndpointConfig
    {
        private static WitEndpointConfig defaultEndpointConfig = new WitEndpointConfig();

        public string uriScheme;
        public string authority;
        public int port;

        public string witApiVersion;

        public string speech;
        public string message;
        public string dictation;

        public string UriScheme => string.IsNullOrEmpty(uriScheme) ? WitConstants.URI_SCHEME : uriScheme;
        public string Authority =>
            string.IsNullOrEmpty(authority) ? WitConstants.URI_AUTHORITY : authority;
        public int Port => port <= 0 ? WitConstants.URI_DEFAULT_PORT : port;
        public string WitApiVersion => string.IsNullOrEmpty(witApiVersion)
            ? WitConstants.API_VERSION
            : witApiVersion;

        public string Speech =>
            string.IsNullOrEmpty(speech) ? WitConstants.ENDPOINT_SPEECH : speech;

        public string Message =>
            string.IsNullOrEmpty(message) ? WitConstants.ENDPOINT_MESSAGE : message;

        public string Dictation => string.IsNullOrEmpty(dictation) ? WitConstants.ENDPOINT_DICTATION : dictation;

        public static WitEndpointConfig GetEndpointConfig(WitConfiguration witConfig)
        {
            return witConfig && null != witConfig.endpointConfiguration
                ? witConfig.endpointConfiguration
                : defaultEndpointConfig;
        }
    }
}
