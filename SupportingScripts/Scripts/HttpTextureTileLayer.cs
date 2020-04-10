// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    /// Provides tiles for a <see cref="TextureTileLayer"/>. The tiles are fetched by using the HTTP or HTTPS protocol.
    /// Results are assumed to be JPEG or PNG, i.e. anything decodable by <see cref="ImageConversion.LoadImage(Texture2D, byte[])"/>.
    /// </summary>
    public class HttpTextureTileLayer : TextureTileLayer
    {
        /// <summary>
        /// The UriFormat property accepts the following case-insensitive replacement strings:
        /// {x}, {y}, {zoomLevel}, and {quadKey}. For more info about these replacement strings,
        /// see Bing Maps Tile System. https://docs.microsoft.com/en-us/bingmaps/articles/bing-maps-tile-system
        /// </summary>
        [SerializeField]
        private string _urlFormatString = "";

        /// <summary>
        /// The UriFormat property accepts the following case-insensitive replacement strings:
        /// {x}, {y}, {zoomLevel}, and {quadKey}. For more info about these replacement strings,
        /// see Bing Maps Tile System. https://docs.microsoft.com/en-us/bingmaps/articles/bing-maps-tile-system
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

        private void OnValidate()
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
            return Task.FromResult<TextureTile?>(TextureTile.FromUrl(new Uri(url)));
        }

        /// <summary>
        /// Can be overriden to apply custom URL formatting logic. The base implementation handles formatting
        /// well known strings: {x}, {y}, {zoomLevel}, and {quadKey}. For more info about these replacement strings,
        /// see Bing Maps Tile System. https://docs.microsoft.com/en-us/bingmaps/articles/bing-maps-tile-system
        /// </summary>
        protected string FormatUrl(TileId tileId)
        {
            var url = _urlFormatString;
            var tilePosition = tileId.ToTilePosition();

            if (url.Contains("{x}"))
            {
                url = url.Replace("{x}", tilePosition.X.ToString());
            }

            if (url.Contains("{y}"))
            {
                url = url.Replace("{y}", tilePosition.Y.ToString());
            }

            if (url.Contains("{zoomLevel}"))
            {
                url = url.Replace("{zoomLevel}", tilePosition.LevelOfDetail.Value.ToString());
            }

            if (url.Contains("{z}"))
            {
                url = url.Replace("{z}", tilePosition.LevelOfDetail.Value.ToString());
            }

            if (url.Contains("{quadKey}"))
            {
                url = url.Replace("{quadKey}", tileId.ToKey());
            }

            return url;
        }
    }
}
