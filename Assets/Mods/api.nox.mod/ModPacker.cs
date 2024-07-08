#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.mod
{
    internal class ModPacker
    {
        internal static bool PackArchive(string path, string id)
        {
            var sourceFolder = FindModSource.FindSource(id);
            if (sourceFolder == null) return false;
            var pathmeta = GMetadata.FindMetadata(id);
            var modMetadata = ModEditorMod._api.LibsAPI.LoadMetadata(pathmeta);
            if (modMetadata == null) return false;

            var pathCompiled = Path.Combine(Application.dataPath, "..", "Library", "NoxModBuild");
            if (!Directory.Exists(pathCompiled))
                Directory.CreateDirectory(pathCompiled);

            var pathAchive = Path.Combine(path, id + ".zip");
            if (File.Exists(pathAchive))
                File.Delete(pathAchive);

            var refFiles = new Dictionary<string, string>();
            foreach (var reference in modMetadata.GetReferences())
            {
                var refPath = Path.Combine(pathCompiled, reference.GetFile());
                if (!File.Exists(refPath)) return false;
                refFiles.Add(reference.GetFile(), refPath);
            }

            var zip = ZipFile.Open(pathAchive, ZipArchiveMode.Create);

            foreach (var reference in modMetadata.GetReferences())
                zip.CreateEntryFromFile(refFiles[reference.GetFile()], "libs/" + reference.GetFile());

            var resources = Path.Combine(sourceFolder, "Resources");
            if (Directory.Exists(resources))
                foreach (var file in Directory.GetFiles(resources, "*", SearchOption.AllDirectories))
                    zip.CreateEntryFromFile(file, "resources/" + file[(resources.Length + 1)..]);

            var entryMetadata = zip.CreateEntry("nox.mod.json");
            using var streamMetadata = entryMetadata.Open();
            streamMetadata.Write(System.Text.Encoding.UTF8.GetBytes(modMetadata.ToJson()));

            zip.Dispose();
            File.Copy(pathAchive, Path.Combine(path, id + ".nmod"));
            File.Delete(pathAchive);
            return true;
        }
    }
}
#endif