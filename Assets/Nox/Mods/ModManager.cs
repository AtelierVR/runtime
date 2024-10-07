using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nox.CCK;
using Nox.Mods.Client;
using Nox.Mods.Type;
using Nox.Scripts;
using UnityEngine;

namespace Nox.Mods
{
    public class ModManager : Manager<RuntimeMod>
    {
        private static string[] _originalPaths = new string[]
        {
            Path.Combine(Constants.GameAppDataPath, "mods"),
            Path.Combine(Application.dataPath, "..", "Library", "NoxMods"),
        };

        private static string[] inpackPaths
        {
            get
            {
                var paths = new HashSet<string>();
                var files = Directory
                    .GetFiles(Application.dataPath, "nox.mod.json*", SearchOption.AllDirectories)
                    .Distinct().ToArray();
                foreach (var file in files)
                {
                    var path = Path.GetDirectoryName(file);
                    if (path != null) paths.Add(path);
                }
                return paths.ToArray();
            }
        }

        public static void Init()
        {

        }

        public static RuntimeMod CreateMod<T>(string path) where T : ModType
        {
            var type = (T)Activator.CreateInstance(typeof(T), path);
            if (!type.Repart()) return null;
            return new RuntimeMod(type.GetMetadata(), type);
        }

        public static string[] GetModPaths()
        {
            var config = Config.Load();
            HashSet<string> paths = new();
            foreach (var path in config.Get("mod_paths", new string[0]))
                if (Directory.Exists(path)) paths.Add(path);
            foreach (var path in _originalPaths)
                if (Directory.Exists(path)) paths.Add(path);
            return new List<string>(paths).ToArray();
        }

        public static ModLoadResult[] LoadAllClientMods()
        {
            List<ModLoadResult> results = new();
            var archives = new List<string>();
            var sources = new List<string>();
            foreach (var path in GetModPaths())
            {
                archives.AddRange(ArchiveMod.GetAllMods(path));
                sources.AddRange(SourceMod.GetAllMods(path));
            }

            var mods = new List<RuntimeMod>();
#if UNITY_EDITOR
            foreach (var path in inpackPaths)
            {
                var mod = CreateMod<InternalMod>(path);
                if (mod != null) mods.Add(mod);
                else results.Add(new ModLoadResult(path, ModLoadResultType.Warning, "Invalid Internal"));
            }
#endif

            foreach (var path in archives)
            {
                var mod = CreateMod<ArchiveMod>(path);
                if (mod != null) mods.Add(mod);
                else results.Add(new ModLoadResult(path, ModLoadResultType.Warning, "Invalid Archive"));
            }

            foreach (var path in sources)
            {
                var mod = CreateMod<SourceMod>(path);
                if (mod != null) mods.Add(mod);
                else results.Add(new ModLoadResult(path, ModLoadResultType.Warning, "Invalid Source"));
            }

            foreach (var mod in mods)
                Debug.Log($"Detected mod {mod.GetMetadata().GetId()}");
            if (results.Where(r => r.IsError).ToArray().Length > 0 || mods.Count == 0) return results.ToArray();

            // check provides
            foreach (var mod in mods)
                foreach (var provide in mod.GetMetadata().GetProvides())
                    foreach (var modMeta in mods)
                        if (mod.GetMetadata().GetId() == provide)
                            results.Add(new ModLoadResult(mod.GetMetadata().GetId(), ModLoadResultType.Error,
                                $"Mod {mod.GetMetadata().GetId()} conflicts with providers."
                            ));
            if (results.Where(r => r.IsError).ToArray().Length > 0) return results.ToArray();

            // check dependencies
            List<RuntimeMod> readyMods = new();
            List<RuntimeMod> breakMods = new();
            foreach (var mod in mods)
            {
                var broken = false;
                foreach (var required in mod.GetMetadata().GetBreaks())
                {
                    var found = false;
                    foreach (var mod2 in mods)
                        if (mod2.GetMetadata().Match(required.GetId()))
                            if (required.GetVersion().Matches(mod2.GetMetadata().GetVersion()))
                            {
                                found = true;
                                break;
                            }
                    if (found)
                    {
                        results.Add(new ModLoadResult(mod.GetMetadata().GetId(), ModLoadResultType.Error,
                            $"Mod {mod.GetMetadata().GetId()} breaks {required.GetId()}{required.GetVersion()} but it is present (0x01)"
                        ));
                        broken = true;
                    }
                }

                foreach (var required in mod.GetMetadata().GetDepends())
                {
                    var found = false;
                    RuntimeMod gameMod = null;
                    foreach (var mod2 in mods)
                        if (mod2.GetMetadata().Match(required.GetId()))
                        {
                            gameMod = mod2;
                            if (required.GetVersion().Matches(mod2.GetMetadata().GetVersion()))
                            {
                                found = true;
                                break;
                            }
                        }
                    if (!found)
                    {
                        results.Add(new ModLoadResult(mod.GetMetadata().GetId(), ModLoadResultType.Error,
                            "Mod " + mod.GetMetadata().GetId()
                            + " depends on " + required.GetId() + required.GetVersion().ToString()
                            + (gameMod != null
                                ? " but it is present as " + gameMod.GetMetadata().GetId() + gameMod.GetMetadata().GetVersion().ToString()
                                : " but it is not present (0x02)"
                        )));
                        broken = true;
                    }
                }

                foreach (var required in mod.GetMetadata().GetConflicts())
                {
                    var found = false;
                    foreach (var mod2 in mods)
                        if (mod2.GetMetadata().Match(required.GetId()))
                            if (required.GetVersion().Matches(mod2.GetMetadata().GetVersion()))
                            {
                                found = true;
                                break;
                            }
                    if (found)
                    {
                        results.Add(new ModLoadResult(mod.GetMetadata().GetId(), ModLoadResultType.Error,
                            "Mod " + mod.GetMetadata().GetId()
                            + " conflicts with " + required.GetId() + required.GetVersion().ToString()
                            + " but it is present (0x03)"
                        ));
                        broken = true;
                    }
                }

                foreach (var required in mod.GetMetadata().GetRecommends())
                {
                    var found = false;
                    RuntimeMod gameMod = null;
                    foreach (var mod2 in mods)
                        if (mod2.GetMetadata().Match(required.GetId()))
                        {
                            gameMod = mod2;
                            if (required.GetVersion().Matches(mod2.GetMetadata().GetVersion()))
                            {
                                found = true;
                                break;
                            }
                        }
                    if (!found)
                    {
                        results.Add(new ModLoadResult(mod.GetMetadata().GetId(), ModLoadResultType.Suggestion,
                            "Mod " + mod.GetMetadata().GetId()
                            + " recommends " + required.GetId() + required.GetVersion().ToString()
                            + (gameMod != null
                                ? " but it is present as " + gameMod.GetMetadata().GetId() + gameMod.GetMetadata().GetVersion().ToString()
                                : " but it is not present (0x04)"
                        )));
                    }
                }

                if (broken)
                    breakMods.Add(mod);
                else readyMods.Add(mod);
            }
            if (results.Where(r => r.IsError).ToArray().Length > 0) return results.ToArray();
            if (breakMods.Count > 0)
            {
                results.AddRange(breakMods.Select(mod => new ModLoadResult(mod.GetMetadata().GetId(), ModLoadResultType.Error,
                    "Mod is broken"
                )));
                return results.ToArray();
            }

            // check namespaces for entry points
            foreach (var mod in mods)
            {
                var mt = mod.GetMetadata();
                var mainEntries = mt.GetEntryPoints().GetMain();
                var clientEntries = mt.GetEntryPoints().GetClient();
                if (mainEntries.Length == 0 && clientEntries.Length == 0) continue;
                var nss = mod.GetModType().GetNamespaces();
                foreach (var me in mainEntries)
                {
                    string ns = null;
                    foreach (var n in nss)
                        if (me.StartsWith(n))
                        {
                            ns = n;
                            break;
                        }
                    if (ns == null)
                    {
                        results.Add(new ModLoadResult(mt.GetId(), ModLoadResultType.Error,
                            $"Namespace for entry point {me} not found"
                        ));
                        continue;
                    }
                }
            }
            if (results.Where(r => r.IsError).ToArray().Length > 0) return results.ToArray();

            // sort mods by dependencies
            mods.Reverse();
            mods.Sort((a, b) =>
            {
                var i = 0;
                foreach (var required in a.GetMetadata().GetDepends())
                    if (b.GetMetadata().Match(required.GetId())) i++;
                foreach (var required in b.GetMetadata().GetDepends())
                    if (a.GetMetadata().Match(required.GetId())) i--;
                return i;
            });

            // load assemblies
            foreach (var mod in mods)
            {
                var mt = mod.GetMetadata();
                var mainEntries = mt.GetEntryPoints().GetMain();
                var clientEntries = mt.GetEntryPoints().GetClient();
                var mc = new List<CCK.Mods.Initializers.ModInitializer>();
                var ec = new List<CCK.Mods.Initializers.ClientModInitializer>();

                var ns = mod.GetModType().GetNamespaces()
                    .Where(n => mainEntries.Any(me => me.StartsWith(n)))
                    .ToArray();
                if (ns.Length > 0)
                    foreach (var n in ns)
                    {
                        var assembly = mod.GetModType().GetAssembly(n);
                        if (assembly == null)
                        {
                            results.Add(new ModLoadResult(mt.GetId(), ModLoadResultType.Error,
                                $"Assembly for namespace (main) {n} not found"
                            ));
                            continue;
                        }
                        foreach (var type in assembly.GetTypes())
                            if (mainEntries.Contains(type.FullName) && type.GetInterface(typeof(CCK.Mods.Initializers.ModInitializer).FullName) != null)
                                mc.Add((CCK.Mods.Initializers.ModInitializer)Activator.CreateInstance(type));
                    }

                ns = mod.GetModType().GetNamespaces()
                    .Where(n => clientEntries.Any(me => me.StartsWith(n)))
                    .ToArray();
                if (ns.Length > 0)
                    foreach (var n in ns)
                    {
                        var assembly = mod.GetModType().GetAssembly(n);
                        if (assembly == null)
                        {
                            results.Add(new ModLoadResult(mt.GetId(), ModLoadResultType.Error,
                                $"Assembly for namespace (client) {n} not found"
                            ));
                            continue;
                        }
                        foreach (var type in assembly.GetTypes())
                            if (clientEntries.Contains(type.FullName) && type.GetInterface(typeof(CCK.Mods.Initializers.ClientModInitializer).FullName) != null)
                                ec.Add((CCK.Mods.Initializers.ClientModInitializer)Activator.CreateInstance(type));
                    }

                if (mc.Count != mainEntries.Length)
                    foreach (var me in mainEntries)
                        if (!mc.Any(m => m.GetType().FullName == me))
                            results.Add(new ModLoadResult(mt.GetId(), ModLoadResultType.Error,
                                $"The main entry point {me} is not found"
                            ));
                if (ec.Count != clientEntries.Length)
                    foreach (var ce in clientEntries)
                        if (!ec.Any(m => m.GetType().FullName == ce))
                            results.Add(new ModLoadResult(mt.GetId(), ModLoadResultType.Error,
                                $"The client entry point {ce} is not found"
                            ));

                mod.SetMainClasses(mc.ToArray());
                mod.SetClientClasses(ec.ToArray());
            }

            if (results.Where(r => r.IsError).ToArray().Length > 0) return results.ToArray();

            // load mods
            foreach (var mod in mods)
            {
                mod.EnableMain();
                mod.EnableClient();
                Add(mod);
            }

            if (results.Where(r => r.IsError).ToArray().Length > 0)
            {
                foreach (var mod in mods)
                {
                    mod.Unload();
                    Remove(mod);
                };
                return results.ToArray();
            }
            
            return mods
                .Select(mod => new ModLoadResult(mod.GetMetadata().GetId(), ModLoadResultType.Success, "Mod loaded"))
                .ToArray();
        }

        public static RuntimeMod GetMod(string id) => Cache.FirstOrDefault(m => m.GetMetadata().Match(id));
        public static RuntimeMod[] GetMods() => Cache.ToArray();

        public static ModMetadata[] DetectedMetadatas()
        {
            List<ModMetadata> metadatas = new();
            foreach (var path in GetMods())
                metadatas.Add(path.GetInternalMetadata());
            return metadatas.ToArray();
        }

        public static void Update()
        {
            foreach (var mod in Cache)
            {
                foreach (var main in mod.GetMainClasses())
                    if (mod.IsMainEnabled()) main.OnUpdate();
                foreach (var client in mod.GetClientClasses())
                    if (mod.IsClientEnabled()) client.OnUpdate();
                foreach (var instance in mod.GetInstanceClasses())
                    if (mod.IsInstanceEnabled()) instance.OnUpdate();
            }
        }

        public static void FixedUpdate()
        {
            foreach (var mod in Cache)
            {
                foreach (var main in mod.GetMainClasses())
                    if (mod.IsMainEnabled()) main.OnFixedUpdate();
                foreach (var client in mod.GetClientClasses())
                    if (mod.IsClientEnabled()) client.OnFixedUpdate();
                foreach (var instance in mod.GetInstanceClasses())
                    if (mod.IsInstanceEnabled()) instance.OnFixedUpdate();
            }
        }

        public static void LateUpdate()
        {
            foreach (var mod in Cache)
            {
                foreach (var main in mod.GetMainClasses())
                    if (mod.IsMainEnabled()) main.OnLateUpdate();
                foreach (var client in mod.GetClientClasses())
                    if (mod.IsClientEnabled()) client.OnLateUpdate();
                foreach (var instance in mod.GetInstanceClasses())
                    if (mod.IsInstanceEnabled()) instance.OnLateUpdate();
            }
        }

        public static void OnApplicationQuit()
        {
            foreach (var mod in Cache)
                mod.Destroy();
        }
    }
}