// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Geospatial;
using Microsoft.Maps.Unity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class CustomElevationTileLayer : ElevationTileLayer
{
    private static readonly string ElevationDataDirectory = Path.Combine(Application.streamingAssetsPath, "CustomElevationData");
    private readonly HashSet<TileId> _tiles = new HashSet<TileId>();

    protected override void Awake()
    {
        base.Awake();

        // Search through custom elevation data directory to find individual elevation tiles.
        // Record which TileIds are present for use in HasElevationTile.
        var elevationDataFiles = Directory.GetFiles(ElevationDataDirectory, "*.et");
        foreach (var elevationDataFile in elevationDataFiles)
        {
            var tileId = TileId.Parse(Path.GetFileNameWithoutExtension(elevationDataFile));
            _tiles.Add(tileId);
        }
    }

    public override Task<bool> HasElevationTile(TileId tileId, CancellationToken cancellationToken = default)
    {
        // We can quickly check tiles are available by using this HashSet which was filled up during Awake().
        return Task.FromResult(_tiles.Contains(tileId));
    }

    public override Task<ElevationTile> GetElevationTileData(TileId tileId, CancellationToken cancellationToken = default)
    {
        if (!_tiles.Contains(tileId))
        {
            return ElevationTile.FromNull();
        }

        // Read the elevation data from the corresponding file in streaming assets. Files are named by the TileId's quadkey.
        var elevationTileData = File.ReadAllBytes(Path.Combine(ElevationDataDirectory, tileId.ToKey() + ".et"));

        // The ElevationTile data has this layout:
        // * 0 - magic id (0x12345678)
        // * 4 - min elevation in meters (float)
        // * 8 - tile elevation range in meters (float)
        // * 12 - normalized elevation values (ushorts - 257*257 entries).
        var minElevationInMeters = BitConverter.ToSingle(elevationTileData, 4);
        var elevationRangeInMeters = BitConverter.ToSingle(elevationTileData, 8);
        var elevationTile =
            ElevationTile.FromNormalizedData(
                tileId,
                257,
                257,
                minElevationInMeters,
                elevationRangeInMeters,
                elevationTileData,
                12 /* offset */);

        // No async code was required so wrap in Task.
        return Task.FromResult(elevationTile);
    }
}
