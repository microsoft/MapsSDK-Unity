/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using System.Reflection;
using Meta.WitAi;

namespace Meta.WitAi.Windows
{
    public class WitEndpointConfigDrawer : WitPropertyDrawer
    {
        // Allow edit with lock
        protected override WitPropertyEditType EditType => WitPropertyEditType.LockEdit;
        // Get default fields
        protected override string GetDefaultFieldValue(SerializedProperty property, FieldInfo subfield)
        {
            // Iterate options
            switch (subfield.Name)
            {
                case "uriScheme":
                    return WitConstants.URI_SCHEME;
                case "authority":
                    return WitConstants.URI_AUTHORITY;
                case "port":
                    return WitConstants.URI_DEFAULT_PORT.ToString();
                case "witApiVersion":
                    return WitConstants.API_VERSION;
                case "speech":
                    return WitConstants.ENDPOINT_SPEECH;
                case "message":
                    return WitConstants.ENDPOINT_MESSAGE;
                case "dictation":
                    return WitConstants.ENDPOINT_DICTATION;
            }

            // Return base
            return base.GetDefaultFieldValue(property, subfield);
        }
        // Use name value for title if possible
        protected override string GetLocalizedText(SerializedProperty property, string key)
        {
            // Iterate options
            switch (key)
            {
                case LocalizedTitleKey:
                    return WitTexts.Texts.ConfigurationEndpointTitleLabel;
                case "uriScheme":
                    return WitTexts.Texts.ConfigurationEndpointUriLabel;
                case "authority":
                    return WitTexts.Texts.ConfigurationEndpointAuthLabel;
                case "port":
                    return WitTexts.Texts.ConfigurationEndpointPortLabel;
                case "witApiVersion":
                    return WitTexts.Texts.ConfigurationEndpointApiLabel;
                case "speech":
                    return WitTexts.Texts.ConfigurationEndpointSpeechLabel;
                case "message":
                    return WitTexts.Texts.ConfigurationEndpointMessageLabel;
                case "dictation":
                    return WitTexts.Texts.ConfigurationEndpointDictationLabel;
            }
            // Default to base
            return base.GetLocalizedText(property, key);
        }
    }
}
