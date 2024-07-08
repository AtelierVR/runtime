#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace api.nox.mod
{
    internal class GameBuilder
    {
        internal static bool CanBuild(BuildTarget target) => target switch
        {
            BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => true,
            BuildTarget.StandaloneLinux64 => true,
            _ => false,
        };

        internal static bool Build(BuildTarget target)
        {
            var path = Path.Combine(Application.dataPath, "..", "Builds");
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new string[0],
                locationPathName = Path.Combine(path, DateTimeOffset.Now.ToUnixTimeSeconds().ToString()),
                target = target,
                options = BuildOptions.None
            });
            return result.summary.result == BuildResult.Succeeded;
        }

        internal static string SelectCompiled(BuildTarget target, string id, string guid)
        {
            var pathCompiled = Path.Combine(Application.dataPath, "..", "Library", "NoxModBuild");
            var pathBuild = Path.Combine(Application.dataPath, "..", "Builds");
            if (!Directory.Exists(pathCompiled))
                Directory.CreateDirectory(pathCompiled);

            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    var pathDll = Directory.GetFiles(pathBuild, id + ".dll", SearchOption.AllDirectories).FirstOrDefault();
                    var pathPdb = Directory.GetFiles(pathBuild, id + ".pdb", SearchOption.AllDirectories).FirstOrDefault();
                    if (pathDll == null || pathPdb == null) return null;
                    var pathDllCompiled = Path.Combine(pathCompiled, guid + ".dll");
                    var pathPdbCompiled = Path.Combine(pathCompiled, guid + ".pdb");
                    if (File.Exists(pathDllCompiled))
                        File.Delete(pathDllCompiled);
                    if (File.Exists(pathPdbCompiled))
                        File.Delete(pathPdbCompiled);
                    File.Copy(pathDll, pathDllCompiled);
                    File.Copy(pathPdb, pathPdbCompiled);
                    return pathDllCompiled;
            }

            return null;
        }
    }
}
#endif