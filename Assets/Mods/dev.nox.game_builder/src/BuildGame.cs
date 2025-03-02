#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nox.CCK.Utils;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace dev.nox.game_builder
{
    public class BuildGame
    {
        public static bool BuildPlayer(Platform target, string outputfolder, string buildname, bool buildScenes = true)
        {
            if (!target.IsSupported())
            {
                Logger.LogError("Unsupported platform: " + target.GetPlatformName());
                return false;
            }

            if (File.Exists(outputfolder))
                File.Delete(outputfolder);

            Directory.CreateDirectory(outputfolder);

            var scenes = Array.Empty<string>();
            if (buildScenes)
            {
                var list = new List<string>();
                foreach (var mod in GameBuilder.CoreAPI.ModAPI.GetMods())
                {
                    var path = mod.GetData("folder", "");
                    if (string.IsNullOrEmpty(path)) continue;
                    var t = Path.Combine(path, "buildscenes");
                    if (!Directory.Exists(t)) continue;
                    var files = Directory.GetFiles(t, "*.unity", SearchOption.AllDirectories)
                            .Select(s => "Assets/" + Path.GetRelativePath(Application.dataPath, s)
                            .Replace("\\", "/")).ToArray();
                    list.AddRange(files);
                }
                list.Sort((a,b) => a.EndsWith("main.unity") ? -1 : 1);
                scenes = list.ToArray();
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputfolder, buildname),
                options = BuildOptions.Development,
                target = target.GetBuildTarget()
            };
            
            var result = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (result.summary.result != BuildResult.Succeeded)
                return false;

            if (target == Platform.Windows)
            {
                Logger.Log("Moving executable to output folder");
                var exe = Path.Combine(outputfolder, buildname + ".exe");
                var baseExec = Path.Combine(outputfolder, buildname);
                if (File.Exists(baseExec))
                    File.Move(baseExec, exe);
            }
            
            return true;
        }
    }
}
#endif