// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.Maps.Unity
{
    using Geospatial;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
#if DEBUG
    using UnityEngine.Assertions;
#endif

    internal class MapPinSpatialIndex
    {
        private class TileData
        {
            internal int MapPinCount;
            internal double TotalLat;
            internal double TotalLon;
            internal ClusterMapPin ClusterMapPin;

            // If non-null, the (unclustered) MapPins for this tile. If null, a cluster exists in this tile and MapPinCount should be
            // greater than the ClusterThreshold.
            internal List<MapPin> MapPins;

            internal bool IsClustered()
            {
                // If the MapPins are null, then we are in clustering mode.
                return MapPins == null;
            }
        }

        private readonly int ClusterThreshold;

        private readonly Dictionary<long, TileData>[] _tiledSpatialIndex;

        private readonly TileLevelOfDetail _maxLod = new TileLevelOfDetail(18);

        private readonly bool _isClusteringEnabled;

        private readonly List<ClusterMapPin> _clusterMapPins = new List<ClusterMapPin>();

        internal MapPinSpatialIndex(bool isClusteringEnabled, int clusterThreshold = 5)
        {
            _isClusteringEnabled = isClusteringEnabled;

            if (clusterThreshold < 2)
            {
                throw new ArgumentException("clusterThreshold should be greater than 1.", nameof(clusterThreshold));
            }

            ClusterThreshold = clusterThreshold;

            // Initialize the spatial index. For each LOD, create a dictionary that maps TileIds to TileData.
            _tiledSpatialIndex = new Dictionary<long, TileData>[_maxLod.Value];
            for (var i = 0; i < _tiledSpatialIndex.Length; i++)
            {
                _tiledSpatialIndex[i] = new Dictionary<long, TileData>();
            }
        }

        internal void AddMapPin(MapPin mapPinToAdd)
        {
            mapPinToAdd.LocationChanged += MapPinLocationChanged;
            var latLon = mapPinToAdd.Location;

            // Insert into max LOD.
            TileId maxLodTileId;
            {
                var lodIndex = _maxLod.Value - 1;
                maxLodTileId = new TileId(latLon, _maxLod);
                if (!_tiledSpatialIndex[lodIndex].TryGetValue(maxLodTileId.Value, out TileData maxLodTileData))
                {
                    maxLodTileData =
                        new TileData
                        {
                            MapPins = new List<MapPin> { mapPinToAdd }
                        };
                    _tiledSpatialIndex[lodIndex].Add(maxLodTileId.Value, maxLodTileData);
                }
                else
                {
                    maxLodTileData.MapPins.Add(mapPinToAdd);
                }

                maxLodTileData.MapPinCount++;
                maxLodTileData.TotalLat += latLon.LatitudeInDegrees;
                maxLodTileData.TotalLon += latLon.LongitudeInDegrees;
            }

            // Bubble up tile into parent LODs.
            maxLodTileId.TryGetParent(out var parentTileId);
            var parentLodIndex = parentTileId.CalculateLevelOfDetail().Value - 1;
            while (parentLodIndex >= 0)
            {
                if (!_tiledSpatialIndex[parentLodIndex].TryGetValue(parentTileId.Value, out var parentTileData))
                {
                    // This is a new tile. Create and add a new TileData for this LOD.
                    parentTileData =
                        new TileData
                        {
                            MapPins = new List<MapPin> { mapPinToAdd },
                            MapPinCount = 1,
                            TotalLat = latLon.LatitudeInDegrees,
                            TotalLon = latLon.LongitudeInDegrees
                        };
                    _tiledSpatialIndex[parentLodIndex].Add(parentTileId.Value, parentTileData);
                }
                else
                {
                    // We already have a tile with points or clusters.

                    // In either case, track the LatLong.
                    parentTileData.MapPinCount++;
                    parentTileData.TotalLat += latLon.LatitudeInDegrees;
                    parentTileData.TotalLon += latLon.LongitudeInDegrees;

                    var isCluster = _isClusteringEnabled && parentTileData.MapPinCount > ClusterThreshold;
                    if (isCluster)
                    {
                        parentTileData.MapPins = null;
                    }
                    else
                    {
                        parentTileData.MapPins.Add(mapPinToAdd);
                    }
                }

                parentLodIndex--;
                parentTileId.TryGetParent(out parentTileId);
            }
        }

        internal void RemoveMapPin(MapPin mapPinToRemove)
        {
            RemoveMapPin(mapPinToRemove, mapPinToRemove.Location);
        }

        private void RemoveMapPin(MapPin mapPinToRemove, LatLon locationOverride)
        {
            // Find the MapPin in the spatial index at the max LOD.
            var lodIndex = _maxLod.Value - 1;
            var maxLodTileId = new TileId(locationOverride, _maxLod);
            var indexChanged = false;
            if (_tiledSpatialIndex[lodIndex].TryGetValue(maxLodTileId.Value, out TileData maxLodTileData))
            {
                if (maxLodTileData.MapPins.Remove(mapPinToRemove))
                {
                    indexChanged = true;

                    mapPinToRemove.LocationChanged -= MapPinLocationChanged;

                    maxLodTileData.MapPinCount--;
                    if (maxLodTileData.MapPinCount == 0)
                    {
                        // Remove tile if now empty.
                        _tiledSpatialIndex[lodIndex].Remove(maxLodTileId.Value);
                    }
                    else
                    {
                        maxLodTileData.TotalLat -= locationOverride.LatitudeInDegrees;
                        maxLodTileData.TotalLon -= locationOverride.LongitudeInDegrees;
                    }
                }
            }

            // Bubble up change to parent tiles.
            if (indexChanged)
            {
                maxLodTileId.TryGetParent(out var parentTileId);
                var parentLodIndex = parentTileId.CalculateLevelOfDetail().Value - 1;
                while (parentLodIndex >= 0)
                {
                    _tiledSpatialIndex[parentLodIndex].TryGetValue(parentTileId.Value, out var parentTileData);
                    parentTileData.MapPinCount--;

                    if (parentTileData.MapPinCount == 0)
                    {
                        // No more pins left in this tile. Remove it from the spatial index.
                        _tiledSpatialIndex[parentLodIndex].Remove(parentTileId.Value);

#if DEBUG
                        // It shouldn't be clustered.
                        Assert.IsTrue(parentTileData.ClusterMapPin == null);
#endif
                    }
                    else
                    {
                        parentTileData.TotalLat -= locationOverride.LatitudeInDegrees;
                        parentTileData.TotalLon -= locationOverride.LongitudeInDegrees;

                        var isCluster = _isClusteringEnabled && parentTileData.MapPinCount > ClusterThreshold;
                        if (!isCluster)
                        {
                            if (parentTileData.MapPins != null)
                            {
                                parentTileData.MapPins.Remove(mapPinToRemove);
                            }
                            else
                            {
                                // When we remove the MapPin, this tile will fall under the cluster threshold so we will need to repopulate
                                // the children list.
                                parentTileData.MapPins = GatherChildren(parentTileId);

                                // Destroy the ClusterMapPin game object if it exists.
                                if (parentTileData.ClusterMapPin != null)
                                {
                                    _clusterMapPins.Remove(parentTileData.ClusterMapPin);
                                    UnityEngine.Object.Destroy(parentTileData.ClusterMapPin.gameObject);
                                    parentTileData.ClusterMapPin = null;
                                }
                            }
                        }
                    }

                    parentLodIndex--;
                    parentTileId.TryGetParent(out parentTileId);
                }
            }
        }

        /// <summary>
        /// Gets the pins in the specified bounding box.
        /// </summary>
        internal void GetPinsInView(
            MercatorBoundingBox mercatorBox,
            float levelOfDetail,
            ClusterMapPin clusterMapPinPrefab,
            Transform parentTransform,
            out List<MapPin> mapPins,
            out List<ClusterMapPin> clusterMapPins)
        {
            var box = mercatorBox.ToGeoBoundingBox();
            var lod = (short)Mathf.Min(Mathf.Round(levelOfDetail), _maxLod.Value);
            var tileLod = new TileLevelOfDetail(lod);
            var tileLodData = _tiledSpatialIndex[lod - 1];
            var tiles = TileOperations.GetCoveredTileIds(box, tileLod);
            mapPins = new List<MapPin>();
            clusterMapPins = new List<ClusterMapPin>();

            foreach (var tile in tiles)
            {
                var tileBounds = tile.ToMercatorBoundingBox();
                var isTileCompletelyInsideMap = mercatorBox.Contains(tileBounds);

                if (tileLodData.TryGetValue(tile.Value, out var tileData))
                {
                    if (tileData.IsClustered())
                    {
                        var latLon = new LatLon(tileData.TotalLat / tileData.MapPinCount, tileData.TotalLon / tileData.MapPinCount);
                        if (isTileCompletelyInsideMap || box.Intersects(latLon))
                        {
                            // Use the ClusterMapPin.
                            if (tileData.ClusterMapPin == null)
                            {
                                // Deactivate the GO to start with. It will get activated once elevation has been sampled and it's in view.
                                clusterMapPinPrefab.gameObject.SetActive(false);
                                var newClusterMapPin = UnityEngine.Object.Instantiate(clusterMapPinPrefab);

                                if (parentTransform != null)
                                {
                                    newClusterMapPin.transform.SetParent(parentTransform, false);
                                }
                                newClusterMapPin.LevelOfDetail = tileLod.Value;
                                tileData.ClusterMapPin = newClusterMapPin;

                                _clusterMapPins.Add(newClusterMapPin);
                            }

                            tileData.ClusterMapPin.Size = tileData.MapPinCount;
                            tileData.ClusterMapPin.Location = new LatLon(latLon.LatitudeInDegrees, latLon.LongitudeInDegrees);
                            clusterMapPins.Add(tileData.ClusterMapPin);
                        }
                    }
                    else
                    {
                        // Add all of the MapPins in this tile to the list.
                        if (isTileCompletelyInsideMap)
                        {
                            foreach (var mapPin in tileData.MapPins)
                            {
                                mapPins.Add(mapPin);
                            }
                        }
                        else
                        {
                            foreach (var mapPin in tileData.MapPins)
                            {
                                if (box.Intersects(mapPin.Location))
                                {
                                    mapPins.Add(mapPin);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the pins in the specified circular area.
        /// </summary>
        internal void GetPinsInView(
            MercatorBoundingBox mercatorBox,
            MercatorBoundingCircle mercatorBoundingCircle,
            float levelOfDetail,
            ClusterMapPin clusterMapPinPrefab,
            Transform parentTransform,
            out List<MapPin> mapPins,
            out List<ClusterMapPin> clusterMapPins)
        {
            var lod = (short)Mathf.Min(Mathf.Round(levelOfDetail), _maxLod.Value);
            var tileLod = new TileLevelOfDetail(lod);
            var tileLodData = _tiledSpatialIndex[lod - 1];
            var tiles = TileOperations.GetCoveredTileIds(mercatorBox, tileLod);
            mapPins = new List<MapPin>();
            clusterMapPins = new List<ClusterMapPin>();

            foreach (var tile in tiles)
            {
                var tileBounds = tile.ToMercatorBoundingBox();
                var isTileCompletelyInsideMap = mercatorBoundingCircle.Contains(tileBounds);

                if (tileLodData.TryGetValue(tile.Value, out var tileData))
                {
                    if (tileData.IsClustered())
                    {
                        var latLon = new LatLon(tileData.TotalLat / tileData.MapPinCount, tileData.TotalLon / tileData.MapPinCount);
                        var mercatorCoordinate = latLon.ToMercatorCoordinate();
                        if (isTileCompletelyInsideMap || mercatorBoundingCircle.Intersects(mercatorCoordinate))
                        {
                            // Use the ClusterMapPin.
                            if (tileData.ClusterMapPin == null)
                            {
                                // Deactivate the GO to start with. It will get activated once elevation has been sampled and it's in view.
                                clusterMapPinPrefab.gameObject.SetActive(false);
                                var newClusterMapPin = UnityEngine.Object.Instantiate(clusterMapPinPrefab);

                                if (parentTransform != null)
                                {
                                    newClusterMapPin.transform.SetParent(parentTransform, false);
                                }
                                newClusterMapPin.LevelOfDetail = tileLod.Value;
                                tileData.ClusterMapPin = newClusterMapPin;

                                _clusterMapPins.Add(newClusterMapPin);
                            }

                            tileData.ClusterMapPin.Size = tileData.MapPinCount;
                            tileData.ClusterMapPin.Location = new LatLon(latLon.LatitudeInDegrees, latLon.LongitudeInDegrees);
                            clusterMapPins.Add(tileData.ClusterMapPin);
                        }
                    }
                    else
                    {
                        // Add all of the MapPins in this tile to the list.
                        if (isTileCompletelyInsideMap)
                        {
                            foreach (var mapPin in tileData.MapPins)
                            {
                                mapPins.Add(mapPin);
                            }
                        }
                        else
                        {
                            foreach (var mapPin in tileData.MapPins)
                            {
                                if (mercatorBoundingCircle.Intersects(mapPin.MercatorCoordinate))
                                {
                                    mapPins.Add(mapPin);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Destroys all ClusterMapPins associated with this index. When pins in view are requested in the future,
        /// the ClusterMapPin will be recreated as needed.
        /// </summary>
        internal void DestroyClusterMapPins()
        {
            foreach (var clusterMapPin in _clusterMapPins)
            {
                if (clusterMapPin != null)
                {
                    UnityEngine.Object.Destroy(clusterMapPin.gameObject);
                }
            }
            _clusterMapPins.Clear();
        }

        private void MapPinLocationChanged(MapPin mapPinToUpdate, LatLon oldLocation)
        {
            RemoveMapPin(mapPinToUpdate, oldLocation);
            AddMapPin(mapPinToUpdate);
        }

        private List<MapPin> GatherChildren(TileId tileId)
        {
            var result = new List<MapPin>();
            var tempChildrenTileIds = new TileId[4];
            var tilesToCheck = new Queue<TileId>();
            tilesToCheck.Enqueue(tileId);

            while (tilesToCheck.Count > 0)
            {
                var tileToCheck = tilesToCheck.Dequeue();
                var lod = tileToCheck.CalculateLevelOfDetail();
                var lodIndex = lod.Value - 1;
                if (_tiledSpatialIndex[lodIndex].TryGetValue(tileToCheck.Value, out var tileData))
                {
                    if (tileData.MapPins != null)
                    {
#if DEBUG
                        if (_isClusteringEnabled && lod != _maxLod)
                        {
                            Assert.IsTrue(tileData.MapPinCount <= ClusterThreshold);
                        }
#endif

                        result.AddRange(tileData.MapPins);
                    }
                    else if (lodIndex < (_maxLod.Value - 1))
                    {
#if DEBUG
                        if (_isClusteringEnabled)
                        {
                            Assert.IsTrue(tileData.MapPinCount <= ClusterThreshold);
                        }
                        Assert.IsTrue(lodIndex < _maxLod.Value);
#endif

                        tileToCheck.GetChildren(tempChildrenTileIds);
                        foreach (var childTile in tempChildrenTileIds)
                        {
                            tilesToCheck.Enqueue(childTile);
                        }
                    }
                }
            }

            return result;
        }
    }
}
