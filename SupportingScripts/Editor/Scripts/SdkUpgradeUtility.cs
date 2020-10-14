// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// This script provides a menu item under "Assets -> Maps SDK for Unity -> Upgrade Component GUIDs". This will find all old component GUIDs
    /// which have had their GUIDs modified to a new value. This is typically a result of migrating a component from a DLL to a script.
    /// Finds and replaces matching GUID usages in any scene or prefab.
    /// </summary>
    public static class SdkUpgradeUtility
    {
        // List of Tuples containing old guid (Item1) and the new guid (Item2).
        private static readonly List<Tuple<string, string>> GuidsToUpdate =
            new List<Tuple<string, string>>
            {
                // MapRenderer
                new Tuple<string, string>(
                    "fileID: -194183520, guid: f58183a31672bf641bbeeaef3d4759c0, type: 3",
                    "fileID: 11500000, guid: 1cf6985fc3c122a4193d16fdcfb59784, type: 3"),
                // CopyrightLayer
                new Tuple<string, string>(
                    "fileID: 1914003294, guid: f58183a31672bf641bbeeaef3d4759c0, type: 3",
                    "fileID: 11500000, guid: 02c7d1b323594a144ac9b98fd93e0f7f, type: 3"),
                // MapDataCache
                new Tuple<string, string>(
                    "fileID: -1554650353, guid: f58183a31672bf641bbeeaef3d4759c0, type: 3",
                    "fileID: 11500000, guid: 1ef50f6f9318ded40b230c1eefb7f968, type: 3"),
                // MapPinLayer
                new Tuple<string, string>(
                    "fileID: 1083584769, guid: f58183a31672bf641bbeeaef3d4759c0, type: 3}",
                    "fileID: 11500000, guid: 5d8a6459eb3010b4f9751de03dca135a, type: 3}"),
                // MapPin
                new Tuple<string, string>(
                    "fileID: -404975296, guid: f58183a31672bf641bbeeaef3d4759c0, type: 3",
                    "fileID: 11500000, guid: 0bb87916f59f52349b237e7ce66a84a1, type: 3"),
                // ClusterMapPin
                new Tuple<string, string>(
                    "fileID: -2137691127, guid: f58183a31672bf641bbeeaef3d4759c0, type: 3",
                    "fileID: 11500000, guid: 10142379db1f9994e9e1ea54ee0ceb78, type: 3")
            };

        /// <summary>
        /// Provides a menu item to automatically update component GUIDs from their old version to their new version.
        /// </summary>
        [MenuItem("Assets/Maps SDK for Unity/Upgrade Component GUIDs")]
        public static void UpgradeScenes()
        {
            // Find all scene files in the assets directory.

            // Replace old GUIDs with new GUIDs.
            var output = "";
            var needsAssetReimport = false;

            // Scenes
            var sceneFiles = Directory.GetFiles(Application.dataPath, "*.unity", SearchOption.AllDirectories);
            foreach (var sceneFile in sceneFiles)
            {
                output += ScrubAssets(sceneFile);
                needsAssetReimport = needsAssetReimport || !string.IsNullOrWhiteSpace(output);
            }

            // Prefabs
            var prefabFiles = Directory.GetFiles(Application.dataPath, "*.prefab", SearchOption.AllDirectories);
            foreach (var prefabFile in prefabFiles)
            {
                output += ScrubAssets(prefabFile);
                needsAssetReimport = needsAssetReimport || !string.IsNullOrWhiteSpace(output);
            }

            // Reimport assets if there were changes to the scene files.
            if (needsAssetReimport)
            {
                Debug.Log("Upgraded assets:\r\n" + output);
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.Log("No assets were modified.");
            }
        }

        private static string ScrubAssets(string assetFile)
        {
            var assetText = File.ReadAllText(assetFile);

            var newAssetText = assetText;
            foreach (var guidToUpdate in GuidsToUpdate)
            {
                newAssetText = newAssetText.Replace(guidToUpdate.Item1, guidToUpdate.Item2);
            }

            var output = "";
            if (newAssetText != assetText)
            {
                output += "    " + Path.GetFileName(assetFile) + "\r\n";
                File.WriteAllText(assetFile, newAssetText);
            }

            return output;
        }
    }
}
