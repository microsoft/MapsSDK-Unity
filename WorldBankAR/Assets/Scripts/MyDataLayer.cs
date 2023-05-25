using UnityEngine;
using Microsoft.Maps.Unity;
using System.Threading.Tasks;
using Microsoft.Geospatial;
using System.Threading;
using UnityEngine.Networking;
using System.Collections;
using System.Net.Http;
using System.IO;
using System.Text;

public class MyDataLayer : HttpTextureTileLayer
{
    public string param, baseUrl;
    private int _taxonKey;
    private TileId _currentTileId;
    private MapRenderer _mapRenderer;

    // "https://api.gbif.org/v2/map/occurrence/density/{z}/{x}/{y}@1x.png?srs=EPSG:3575&publishingCountry=SE&basisOfRecord=PRESERVED_SPECIMEN&basisOfRecord=FOSSIL_SPECIMEN&basisOfRecord=LIVING_SPECIMEN&year=1600,1899&bin=square&squareSize=128&style=red.poly";
    // @1x.png?taxonKey=212&basisOfRecord=MACHINE_OBSERVATION&years=2015,2017&bin=square&squareSize=128&style=purpleYellow-noborder.poly
    // @1x.png?taxonKey=212&bin=hex&hexPerTile=30&style=classic-noborder.poly
    //../../../map/occurrence/density/{z}/{x}/{y}@2x.png?srs=EPSG:4326&bin=hex&hexPerTile=154&taxonKey=2480505&style=classic.poly

    protected override void Awake()
    {
        base.Awake();
        _mapRenderer = GetComponent<MapRenderer>();
        baseUrl = "https://api.gbif.org/v2/map/occurrence/density/";
        _taxonKey = 1;
        param = "&bin=hex&hexPerTile=30&style=purpleYellow-noborder.poly";
    }

    public void SetTaxonKey(int nubKey)
    {
        _taxonKey = nubKey;
        UrlFormatString = ComposeUrl();
    }

    public override async Task<TextureTile?> GetTexture(TileId tileId, CancellationToken cancellationToken = default)
    {
        _currentTileId = tileId;

        Task<byte[]> task = LoadImageAsync(ComposeUrl());
        byte[] imageData = await task;

        return TextureTile.FromImageData(imageData);
    }

    public static async Task<byte[]> LoadImageAsync(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                throw new HttpRequestException($"## Failed to load image from {url}. Status code: {response.StatusCode}");
            }
        }
    }

    private string ComposeUrl()
    {
        var z = Mathf.RoundToInt(_mapRenderer.ZoomLevel);
        var x = _currentTileId.ToTilePosition().X;
        var y = _currentTileId.ToTilePosition().Y;

        var url = Path.Combine(baseUrl, z.ToString(), x.ToString(), y.ToString());
        StringBuilder sb = new StringBuilder(url);
        sb.Append("@1x.png?taxonKey=");
        sb.Append(_taxonKey);
        sb.Append(param);

        Debug.Log("### url " + sb.ToString());
        return sb.ToString();
    }
}
