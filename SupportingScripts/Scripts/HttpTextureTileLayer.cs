// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Networking;

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

        private TaskScheduler _unityTaskScheduler;

        /// <summary>
        /// Overriden from base class. Because it is called on Unity main thread, it is used to cache the Unity TaskScheduler.
        /// </summary>
        protected override void OnEnable()
        {
            base.Awake();

            _unityTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        private void OnValidate()
        {
            SetDirty();
        }

        /// <summary>
        /// Retrieves the texture data that will be rendered for the specified <see cref="TileId"/>.
        /// </summary>
        public async override Task<TextureTile> GetTexture(
            TileId tileId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(_urlFormatString))
            {
                return null;
            }

            var url = FormatUrl(tileId);
            var data =
                await
                    // Get the request initiated on Unity thread since this is required by the UnityWebRequest API.
                    await Task.Factory.StartNew(
                        async () => await Request(url).ConfigureAwait(false),
                        cancellationToken,
                        TaskCreationOptions.None,
                        _unityTaskScheduler);

            // We should be on the Unity main thread now, but we don't need to be.
            // Return the result on a background thread to get it off the Unity main thread.
            return await Task.Run(() => Task.FromResult(TextureTile.FromImageData(data))).ConfigureAwait(false);
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

        private async Task<byte[]> Request(string url)
        {
            var webRequest = UnityWebRequest.Get(url);
            await webRequest.SendWebRequest();

            // Check error codes and parse the data. Note, we're on the Unity main thread here.

            if (webRequest.isNetworkError)
            {
#if DEBUG
                Debug.LogError(nameof(HttpTextureTileLayer) + ": Network error - " + url);
#endif
                return null;
            }
            else if (webRequest.isHttpError)
            {
#if DEBUG
                Debug.LogError(nameof(HttpTextureTileLayer) + ": HTTP error " + webRequest.responseCode + " - " + url);
#endif

                if (webRequest.responseCode == 401)
                {
                    return null;
                }
                else if (webRequest.responseCode >= 500)
                {
                    return null;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return webRequest.downloadHandler.data;
            }
        }
    }
}
