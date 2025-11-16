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
using LogType = UnityEngine.LogType;

namespace dev.nox.game_builder
{
    public class BuildGame
    {
        /// <summary>
        /// Méthode compatible avec Unity Cloud Build et GitHub Actions.
        /// Peut être appelée via: Unity -quit -batchmode -executeMethod dev.nox.game_builder.BuildGame.PerformBuild
        /// </summary>
        public static void PerformBuild()
        {
            // Parse arguments from command line
            var args = ParseCommandLineArguments();
            
            // Get build target from command line or use current
            var buildTarget = args.TryGetValue("buildTarget", out var arg) 
                ? GetBuildTargetFromString(arg) 
                : EditorUserBuildSettings.activeBuildTarget;
            
            var platform = buildTarget.GetPlatform();
            
            // Get output path
            var outputPath = args.ContainsKey("customBuildPath") 
                ? args["customBuildPath"] 
                : "Builds";
            
            // Get build name
            var buildName = args.ContainsKey("customBuildName") 
                ? args["customBuildName"] 
                : Application.productName;
            
            // Get build options
            var buildOptions = ParseBuildOptions(args);
            
            // Determine if we should build scenes
            var buildScenes = !args.ContainsKey("skipScenes") || args["skipScenes"] != "true";
            
            Debug.Log($"[BuildGame] Starting build for platform: {platform.GetPlatformName()}");
            Debug.Log($"[BuildGame] Output path: {outputPath}");
            Debug.Log($"[BuildGame] Build name: {buildName}");
            Debug.Log($"[BuildGame] Build options: {buildOptions}");
            
            var success = BuildPlayer(platform, outputPath, buildName, buildScenes, buildOptions);
            
            if (!success)
            {
                Debug.LogError("[BuildGame] Build failed!");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("[BuildGame] Build completed successfully!");
                EditorApplication.Exit(0);
            }
        }
        
        /// <summary>
        /// Main build method with configurable options
        /// </summary>
        public static bool BuildPlayer(Platform target, string outputfolder, string buildname, bool buildScenes = true, BuildOptions buildOptions = BuildOptions.None)
        {
            if (!target.IsSupported())
            {
                Logger.LogError("Unsupported platform: " + target.GetPlatformName());
                return false;
            }

            if (File.Exists(outputfolder))
                File.Delete(outputfolder);

            Directory.CreateDirectory(outputfolder);

            var scenes = GetScenesToBuild(buildScenes);

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputfolder, buildname),
                options = buildOptions,
                target = target.GetBuildTarget()
            };
            
            Debug.Log($"[BuildGame] Building with {scenes.Length} scene(s)");
            foreach (var scene in scenes)
            {
                Debug.Log($"[BuildGame] - Scene: {scene}");
            }
            
            var result = BuildPipeline.BuildPlayer(buildPlayerOptions);

            Debug.Log($"[BuildGame] Build result: {result.summary.result}");
            Debug.Log($"[BuildGame] Total size: {result.summary.totalSize} bytes");
            Debug.Log($"[BuildGame] Total time: {result.summary.totalTime}");
            
            if (result.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildGame] Build failed with {result.summary.totalErrors} error(s)");
                foreach (var step in result.steps) {
                    if (step.messages.Length <= 0) continue;
                    foreach (var message in step.messages)
                        if (message.type is LogType.Error or LogType.Exception)
                            Debug.LogError($"[BuildGame] {message.content}");
                }
                return false;
            }

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
        
        private static string[] GetScenesToBuild(bool buildScenes)
        {
            if (!buildScenes)
            {
                // Use scenes from build settings
                return EditorBuildSettings.scenes
                    .Where(s => s.enabled)
                    .Select(s => s.path)
                    .ToArray();
            }
            
            var list = new List<string>();
            
            try
            {
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
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildGame] Could not load mod scenes: {ex.Message}");
                Debug.LogWarning("[BuildGame] Falling back to build settings scenes");
                return EditorBuildSettings.scenes
                    .Where(s => s.enabled)
                    .Select(s => s.path)
                    .ToArray();
            }
            
            return list.ToArray();
        }
        
        private static Dictionary<string, string> ParseCommandLineArguments()
        {
            var args = new Dictionary<string, string>();
            var commandLineArgs = Environment.GetCommandLineArgs();
            
            for (var i = 0; i < commandLineArgs.Length; i++) {
                if (!commandLineArgs[i].StartsWith("-") || i + 1 >= commandLineArgs.Length) continue;
                var key   = commandLineArgs[i].TrimStart('-');
                var value = commandLineArgs[i + 1];
                args[key] = value;
            }
            
            return args;
        }
        
        private static BuildTarget GetBuildTargetFromString(string targetString)
        {
            // Support common Unity build target names
            return targetString.ToLower() switch
            {
                "win64" or "windows64" or "standalonewindows64" => BuildTarget.StandaloneWindows64,
                "linux64" or "standalonelinux64" => BuildTarget.StandaloneLinux64,
                "osx" or "macos" or "standaloneosx" => BuildTarget.StandaloneOSX,
                "android" => BuildTarget.Android,
                "ios" => BuildTarget.iOS,
                "visionos" => BuildTarget.VisionOS,
                _ => EditorUserBuildSettings.activeBuildTarget
            };
        }
        
        private static BuildOptions ParseBuildOptions(Dictionary<string, string> args)
        {
            var options = BuildOptions.None;
            
            // Development build
            if (args.ContainsKey("development") && args["development"] == "true")
            {
                options |= BuildOptions.Development;
            }
            
            // Allow debugging
            if (args.ContainsKey("allowDebugging") && args["allowDebugging"] == "true")
            {
                options |= BuildOptions.AllowDebugging;
            }
            
            // Compressed asset bundle
            if (args.ContainsKey("compressAssetBundle") && args["compressAssetBundle"] == "true")
            {
                options |= BuildOptions.CompressWithLz4;
            }
            
            // Auto-connect profiler
            if (args.ContainsKey("autoConnectProfiler") && args["autoConnectProfiler"] == "true")
            {
                options |= BuildOptions.ConnectWithProfiler;
            }
            
            // Enable deep profiling
            if (args.ContainsKey("enableDeepProfilingSupport") && args["enableDeepProfilingSupport"] == "true")
            {
                options |= BuildOptions.EnableDeepProfilingSupport;
            }
            
            return options;
        }
    }
}
#endif