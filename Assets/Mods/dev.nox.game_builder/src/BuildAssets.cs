#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nox.CCK.Mods;
using Nox.CCK.Utils;
using UnityEditor;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace dev.nox.game_builder
{
    public class BuildAssets
    {
        public static string[] BuildAsAssetBundle(Mod mod, Platform target, string outputfolder, string buildname)
        {
            if (!target.IsSupported())
            {
                Logger.LogError("Unsupported platform: " + target.GetPlatformName());
                return null;
            }

            var assetfolder = mod.GetData<string>("assets");

            if (!Directory.Exists(assetfolder))
            {
                Logger.LogError("Asset folder not found: " + assetfolder);
                return null;
            }
            var files = Directory.GetFiles(assetfolder, buildname + ".*", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
                File.Delete(file);

            var buildTarget = target.GetBuildTarget();
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);

            var scenes = Directory.GetFiles(assetfolder, "*.unity", SearchOption.AllDirectories);
            var scene_assets = scenes.Select(s => "Assets/" + Path.GetRelativePath(Application.dataPath, s).Replace("\\", "/")).ToArray();

            var assets = Directory.GetFiles(assetfolder, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".meta") && !f.EndsWith(".unity"))
                .Select(f => "Assets/" + Path.GetRelativePath(Application.dataPath, f).Replace("\\", "/")).ToArray();

            var manifest = BuildPipeline.BuildAssetBundles(new()
            {
                outputPath = outputfolder,
                targetPlatform = target.GetBuildTarget(),
                options = BuildAssetBundleOptions.None,
                bundleDefinitions = new AssetBundleBuild[]
                {
                    new ()
                    {
                        assetBundleName = buildname + ".scenes",
                        assetNames = scene_assets,
                    },
                    new ()
                    {
                        assetBundleName = buildname + ".assets",
                        assetNames = assets,
                    },
                }
            });

            return manifest?.GetAllAssetBundles()
                .Select(b => Path.Combine(outputfolder, b))
                .ToArray();
        }

        public static AssetBundleBuildReult[] BuildAsAssetBundles(Mod[] mod, Platform target, string outputfolder)
        {
            if (!target.IsSupported())
            {
                Logger.LogError("Unsupported platform: " + target.GetPlatformName());
                return new AssetBundleBuildReult[0];
            }

            List<AssetBundleBuild> bundles = new();
            List<AssetBundleBuildReult> results = new();

            for (int i = 0; i < mod.Length; i++)
            {
                var buildname = mod[i].GetMetadata().GetId();
                var assetfolder = mod[i].GetData<string>("assets");

                if (!Directory.Exists(assetfolder)) continue;

                Logger.Log("Building asset bundles for: " + buildname);

                var scenes = Directory.GetFiles(assetfolder, "*.unity", SearchOption.AllDirectories)
                    .Select(f => "Assets/" + Path.GetRelativePath(Application.dataPath, f).Replace("\\", "/")).ToArray();

                var scriptables = Directory.GetFiles(assetfolder, "*.asset", SearchOption.AllDirectories)
                    .Select(f => "Assets/" + Path.GetRelativePath(Application.dataPath, f).Replace("\\", "/")).ToArray();

                var assets = Directory.GetFiles(assetfolder, "*.*", SearchOption.AllDirectories)
                    .Where(f => !f.EndsWith(".meta") && !f.EndsWith(".unity") && !f.EndsWith(".asset"))
                    .Select(f => "Assets/" + Path.GetRelativePath(Application.dataPath, f).Replace("\\", "/")).ToArray();


                foreach (var asset in assets)
                    Logger.Log($"Asset for {mod[i].GetMetadata().GetId()}: {asset}");

                var OutSceneName = Guid.NewGuid().ToString().Replace("-", "") + ".scenes";
                bundles.Add(new AssetBundleBuild
                {
                    assetBundleName = OutSceneName,
                    assetNames = scenes,
                });

                var OutAssetName = Guid.NewGuid().ToString().Replace("-", "") + ".assets";
                bundles.Add(new AssetBundleBuild
                {
                    assetBundleName = OutAssetName,
                    assetNames = assets,
                });

                var OutScriptableName = Guid.NewGuid().ToString().Replace("-", "") + ".scriptables";
                bundles.Add(new AssetBundleBuild
                {
                    assetBundleName = OutScriptableName,
                    assetNames = scriptables,
                });

                results.Add(new AssetBundleBuildReult
                {
                    mod = mod[i],
                    outputs = new string[]
                    {
                        Path.Combine(outputfolder, OutSceneName),
                        Path.Combine(outputfolder, OutAssetName),
                        Path.Combine(outputfolder, OutScriptableName),
                    }
                });
            }

            var manifest = BuildPipeline.BuildAssetBundles(new()
            {
                outputPath = outputfolder,
                targetPlatform = target.GetBuildTarget(),
                options = BuildAssetBundleOptions.None | BuildAssetBundleOptions.IgnoreTypeTreeChanges | BuildAssetBundleOptions.RecurseDependencies,
                bundleDefinitions = bundles.ToArray()
            });

            if (manifest == null)
            {
                Logger.LogError("Failed to build asset bundles");
                return null;
            }

            var expected = results.SelectMany(r => r.outputs.Select(o => Path.GetFileName(o)));
            if (manifest.GetAllAssetBundles().Length != expected.Count())
            {
                Logger.LogWarning("Asset bundles count mismatch");
                Logger.LogWarning($"Expected: ({expected.Count()})[{string.Join(", ", expected)}]");
                Logger.LogWarning($"Got: ({manifest.GetAllAssetBundles().Length})[{string.Join(", ", manifest.GetAllAssetBundles())}]");
                var missing = expected.Except(manifest.GetAllAssetBundles());
                Logger.LogWarning($"Missing: ({missing.Count()})[{string.Join(", ", missing)}]");
            }

            return results.ToArray();
        }

        public class AssetBundleBuildReult
        {
            public Mod mod;
            public string[] outputs;
        }
    }
}
#endif