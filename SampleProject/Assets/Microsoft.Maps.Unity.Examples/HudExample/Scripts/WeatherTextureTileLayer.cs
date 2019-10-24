// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Geospatial;
using Microsoft.Maps.Unity;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A <see cref="TextureTileLayer"/> for displaying weather data from https://mesonet.agron.iastate.edu/ogc/.
/// </summary>
public class WeatherTextureTileLayer : TextureTileLayer
{
    private HttpClient _httpClient;

    protected override void OnEnable()
    {
        base.OnEnable();
        _httpClient = new HttpClient();
    }

    public async override Task<TextureTile> GetTexture(TileId tileId, CancellationToken cancellationToken = default)
    {
        // This tile service works in TilePositions (X, Y, ZOOM), not TileIds, so we need to convert.
        var tilePosition = tileId.ToTilePosition();
        var zoom = tilePosition.LevelOfDetail.Value;

        // Also, the service has four DNS aliases which can help distribute request load. Because this is a quad-tree
        // tile system where each tile has three siblings, i.e. a tile has four children, then we can use the tile's ID
        // in relation to it's parent (is it child 0, 1, 2, or 3?) to determine which subdomain alias to use.
        var subdomain = tileId.GetSubdomain();
        var subdomainId = subdomain == 0 ? "" : subdomain.ToString();

        // Construct the URL.
        var uri = $@"http://mesonet{subdomainId}.agron.iastate.edu/cache/tile.py/1.0.0/nexrad-n0q-900913/{zoom}/{tilePosition.X}/{tilePosition.Y}.jpg";

        // Retrieve the bytes for this tile (it's a JPEG).
        var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);
        var result = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

        // Create a TextureTile.
        return TextureTile.FromImageData(result);
    }
}
