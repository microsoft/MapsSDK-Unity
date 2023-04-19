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
    private MapRenderer _mapRenderer;

    protected override void Awake()
    {
        base.Awake();
        _mapRenderer = GetComponent<MapRenderer>();
    }

    public override async Task<TextureTile?> GetTexture(TileId tileId, CancellationToken cancellationToken = default)
    {
        var z = _mapRenderer.ZoomLevel; // tileId.CalculateLevelOfDetail().Value;
        var x = tileId.ToTilePosition().X;
        var y = tileId.ToTilePosition().Y;

        // "https://api.gbif.org/v2/map/occurrence/density/{z}/{x}/{y}@1x.png?srs=EPSG:3575&publishingCountry=SE&basisOfRecord=PRESERVED_SPECIMEN&basisOfRecord=FOSSIL_SPECIMEN&basisOfRecord=LIVING_SPECIMEN&year=1600,1899&bin=square&squareSize=128&style=red.poly";
        // @1x.png?taxonKey=212&basisOfRecord=MACHINE_OBSERVATION&years=2015,2017&bin=square&squareSize=128&style=purpleYellow-noborder.poly
        // @1x.png?taxonKey=212&bin=hex&hexPerTile=30&style=classic-noborder.poly
        string baseUrl = "https://api.gbif.org/v2/map/occurrence/density/";
        string param = "@2x.png?taxonKey=212&basisOfRecord=MACHINE_OBSERVATION&years=2015,2017&bin=square&squareSize=128&style=purpleYellow-noborder.poly";

        var url = Path.Combine(baseUrl, z.ToString(), x.ToString(), y.ToString());
        StringBuilder sb = new StringBuilder(url);
        sb.Append(param);

        Debug.Log(sb.ToString());

        Task<byte[]> task = LoadImageAsync(sb.ToString());
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


}
