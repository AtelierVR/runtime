using System.Collections.Generic;
using System.IO;
using Nox.CCK;

namespace Nox.Mods
{
    // a mod is 2 possible things:
    //   - is a folder with a mod.json(c) and asmdef
    //   - is a zip (.nowm) file with a mod.json(c) and a dll file

    // is is in Editor, check Assets/ and Mods/ for mods
    // for all possiblititises, check in %appdata%/.avr/config.json, and check for mods in the mods folder and mods/ folder

    public class ModDetector
    {
        public static string GetDefaultPath() => Path.Combine(CCK.Constants.GameAppDataPath, "config.json");
        public static List<string> GetListOfPaths()
        {
            List<string> paths = new() { GetDefaultPath() };
#if UNITY_EDITOR
            paths.Add("Assets/");
            paths.Add("Mods/");
#endif
            var config = Config.Load();
            foreach (var path in config.Get("mod_paths", new string[] { }))
                paths.Add(path);
            return paths;
        }

        public static List<DetectedModUncompressed> ListOfUnCompressedMods()
        {
            List<DetectedModUncompressed> mods = new();
            foreach (var path in GetListOfPaths())
                if (Directory.Exists(path))
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        var files = Directory.GetFiles(dir);
                        if (files.Length == 0) continue;
                        var metadatafile = "";
                        var executable = "";
                        foreach (var file in files)
                        {
                            if (file == "mod.json" || file == "mod.jsonc") metadatafile = file;
                            if (file.EndsWith(".dll")) executable = file;
                            if (metadatafile != "" && executable != "")
                            {
                                mods.Add(new DetectedModUncompressed
                                {
                                    OriginPath = path,
                                    Path = dir,
                                    Executable = executable,
                                    MetaData = metadatafile
                                });
                                break;
                            }
                        }
                    }
            return mods;
        }

        public static List<DetectedMod> ListOfMods()
        {
            List<DetectedMod> mods = new();
            foreach (var path in GetListOfPaths())
                if (Directory.Exists(path))
                    foreach (var file in Directory.GetFiles(path))
                        if (file.EndsWith(".nowm"))
                            mods.Add(new DetectedMod { Path = file });
            return mods;
        }

        public static List<DetectedModUnCompiled> ListOfUnCompiledMods()
        {
            List<DetectedModUnCompiled> mods = new();
            foreach (var path in GetListOfPaths())
                if (Directory.Exists(path))
                {
                    var files = Directory.GetFiles(path);
                    if (files.Length == 0) continue;
                    var metadatafile = "";
                    foreach (var file in files)
                    {
                        if (file == "mod.json" || file == "mod.jsonc") metadatafile = file;
                        if (metadatafile != "")
                        {
                            mods.Add(new DetectedModUnCompiled
                            {
                                OriginPath = path,
                                Path = file,
                                MetaData = metadatafile
                            });
                            break;
                        }
                    }
                }
            return mods;
        }
    }

    public class DetectedModUncompressed
    {
        public string OriginPath { get; set; }
        public string Path { get; set; }
        public string Executable { get; set; }
        public string MetaData { get; set; }
    }

    public class DetectedMod
    {
        public string Path { get; set; }
    }

    public class DetectedModUnCompiled
    {
        public string OriginPath { get; set; }
        public string Path { get; set; }
        public string MetaData { get; set; }
    }
}