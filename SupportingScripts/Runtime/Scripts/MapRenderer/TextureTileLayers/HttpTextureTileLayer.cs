// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    /// Provides tiles for a <see cref="TextureTileLayer"/>. The tiles are fetched by using the HTTP or HTTPS protocol.
    /// Results are assumed to be JPEG or PNG, i.e. anything decodable by <see cref="ImageConversion.LoadImage(Texture2D, byte[])"/>.
    /// </summary>
    public class HttpTextureTileLayer : TextureTileLayer
    {
        private bool _hasWarnedMissingSubdomain;
        private int _subdomainCounter;

        /// <summary>
        /// The UriFormat property accepts the following case-insensitive replacement strings:
        /// {x}, {y}, {zoomlevel}/{zoom}/{z}, {quadkey}, and {subdomain}.
        /// </summary>
        [SerializeField]
        private string _urlFormatString = "";

        /// <summary>
        /// The UriFormat property accepts the following case-insensitive replacement strings:
        /// {x}, {y}, {zoomlevel}/{zoom}/{z}, {quadkey}, and {subdomain}.
        /// </summary>
        public string UrlFormatString
        {
            get => _urlFormatString;
            set
            {
                if (value != _urlFormatString)
                {
                    _urlFormatString = value;
                    SetDirty();
                }
            }
        }

        /// <summary>
        /// A list of subdomains that can be formatted into the URL.
        /// </summary>
        [SerializeField]
        private List<string> _subdomains = new List<string>();

        /// <summary>
        /// A list of subdomains that can be formatted into the URL.
        /// </summary>
        public IList<string> Subdomains => _subdomains;

        /// <summary>
        /// This method is called when the script is loaded or a value is changed in the inspector. Called in the editor only.
        /// </summary>
        protected void OnValidate()
        {
            SetDirty();
        }

        /// <summary>
        /// Retrieves the texture data that will be rendered for the specified <see cref="TileId"/>.
        /// </summary>
        public override Task<TextureTile?> GetTexture(
            TileId tileId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(_urlFormatString))
            {
                return TextureTile.FromNull();
            }

            var url = FormatUrl(tileId);
            if (string.IsNullOrWhiteSpace(url))
            {
                return TextureTile.FromNull();
            }

            return Task.FromResult<TextureTile?>(TextureTile.FromUrl(new Uri(url)));
        }

        /// <summary>
        /// Can be overriden to apply custom URL formatting logic. The base implementation handles formatting
        /// the following case-insensitive replacement strings: {x}, {y}, {zoomlevel}/{zoom}/{z}, {quadkey}, and {subdomain}.
        /// </summary>
        protected string FormatUrl(TileId tileId)
        {
            var tilePosition = tileId.ToTilePosition();

            var urlStringBuilder = new System.Text.StringBuilder(_urlFormatString.Length);

            var startIndex = 0;
            var lastEndIndex = 0;
            while ((startIndex = _urlFormatString.IndexOf('{', startIndex)) >= 0)
            {
                urlStringBuilder.Append(_urlFormatString, lastEndIndex, startIndex - lastEndIndex);

                var placeholderStartIndex = startIndex + 1;
                var endIndex = _urlFormatString.IndexOf('}', placeholderStartIndex);
                var placeholderCount = endIndex - startIndex - 1;

                if (endIndex == -1)
                {
                    // Couldn't find a closing '}'. Keep it in the resulting URL, but it's probably malformed already.
                    lastEndIndex = startIndex;
                    break;
                }

                if (placeholderCount > 0)
                {
                    if (string.Compare(_urlFormatString, placeholderStartIndex, "x", 0, placeholderCount, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        urlStringBuilder.Append(tilePosition.X.ToString(CultureInfo.InvariantCulture));
                    }
                    else if (string.Compare(_urlFormatString, placeholderStartIndex, "y", 0, placeholderCount, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        urlStringBuilder.Append(tilePosition.Y.ToString(CultureInfo.InvariantCulture));
                    }
                    else if (string.Compare(_urlFormatString, placeholderStartIndex, "z", 0, placeholderCount, StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(_urlFormatString, placeholderStartIndex, "zoom", 0, placeholderCount, StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(_urlFormatString, placeholderStartIndex, "zoomlevel", 0, placeholderCount, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        urlStringBuilder.Append(tilePosition.LevelOfDetail.Value.ToString(CultureInfo.InvariantCulture));
                    }
                    else if (string.Compare(_urlFormatString, placeholderStartIndex, "quadkey", 0, placeholderCount, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        urlStringBuilder.Append(tileId.ToKey());
                    }
                    else if (string.Compare(_urlFormatString, placeholderStartIndex, "subdomain", 0, placeholderCount, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (_subdomains != null && _subdomains.Count > 0)
                        {
                            var subdomain = _subdomains[_subdomainCounter % _subdomains.Count];
                            _subdomainCounter++;

                            urlStringBuilder.Append(subdomain);
                        }
                        else
                        {
                            if (!_hasWarnedMissingSubdomain)
                            {
                                _hasWarnedMissingSubdomain = true;
                                Debug.LogWarning("URL uses a {subdomain} format specified but no subdomains were provided: " + _urlFormatString);
                            }
                            return null;
                        }
                    }
                }

                startIndex = endIndex + 1;
                lastEndIndex = startIndex;
            }

            urlStringBuilder.Append(_urlFormatString, lastEndIndex, _urlFormatString.Length - lastEndIndex);

            var formattedUrl = urlStringBuilder.ToString();
            return formattedUrl;
        }
    }
}
