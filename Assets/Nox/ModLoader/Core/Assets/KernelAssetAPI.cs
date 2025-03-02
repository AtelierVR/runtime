using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Logger = Nox.CCK.Utils.Logger;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

#if !UNITY_EDITOR
using Newtonsoft.Json.Linq;
#endif


namespace Nox.ModLoader.Cores.Assets
{
    public class KernelAssetAPI : AssetAPI
    {
        private readonly ModLoader.Mods.KernelMod kernelMod;
        public KernelAssetAPI(ModLoader.Mods.KernelMod kernelMod) { this.kernelMod = kernelMod; }

        public bool HasAsset<T>(string name) where T : Object => HasAsset<T>(kernelMod.Metadata.GetId(), name);
        public bool HasAsset<T>(string ns, string name) where T : Object
        {
            if (HasOverrideAsset<T>(ns, name))
                return true;

            // get on other mods
            if (ModManager.Mods.Where(m => m != kernelMod && m.IsLoaded() && m.GetMetadata().Match(ns)).Any(m => m.AssetAPI.HasOverrideAsset<T>(ns, name)))
                return true;
            
            // get from the initial mod
            var mod = ModManager.GetMod(ns);
            return mod != null && mod.AssetAPI.HasLocalAsset<T>(name);
        }

        public T GetAsset<T>(string name) where T : Object => GetAsset<T>(kernelMod.Metadata.GetId(), name);
        public T GetAsset<T>(string ns, string name) where T : Object
        {
            // get on override mod
            if (HasOverrideAsset<T>(ns, name))
                return GetOverrideAsset<T>(ns, name);

            // get on other mods
            foreach (var m in ModManager.Mods.Where(m => m != kernelMod && m.IsLoaded() && m.GetMetadata().Match(ns)).Where(m => m.AssetAPI.HasOverrideAsset<T>(ns, name)))
                return m.AssetAPI.GetOverrideAsset<T>(ns, name);
            

            // get from the initial mod
            var mod = ModManager.GetMod(ns);
            return mod?.AssetAPI.GetLocalAsset<T>(name);
        }

        public async UniTask<Scene> LoadWorld(string name, LoadSceneMode mode = LoadSceneMode.Single) => await LoadWorld(kernelMod.Metadata.GetId(), name, mode);
        public async UniTask<Scene> LoadWorld(string ns, string name, LoadSceneMode mode = LoadSceneMode.Single)
        {
            Logger.LogDebug("Kernel Loading world: " + ns + "/" + name);
            // load on override mod
            if (IsLoadedWorld(ns, name))
            {
                Logger.LogDebug("Kernel is loaded: " + ns + "/" + name);
                return GetWorld(ns, name);
            }

            if (HasOverrideWorld(ns, name))
            {
                Logger.LogDebug("Kernel has override: " + ns + "/" + name);
                return await LoadOverrideWorld(ns, name, mode);
            }

            // load on other mods
            foreach (var m in ModManager.Mods.Where(m => m != kernelMod && m.IsLoaded() && m.GetMetadata().Match(ns)).Where(m => m.AssetAPI.HasOverrideWorld(ns, name)))
            {
                Logger.LogDebug("Kernel loading from other mod: " + ns + "/" + name);
                return await m.AssetAPI.LoadOverrideWorld(ns, name, mode);
            }
            // load from the initial mod
            var mod = ModManager.GetMod(ns);
            if (mod != null)
            {
                Logger.LogDebug("Kernel loading from initial mod: " + ns + "/" + name);
                return await mod.AssetAPI.LoadLocalWorld(name, mode);
            }
            return default;
        }

        public bool HasWorld(string name) => HasWorld(kernelMod.Metadata.GetId(), name);
        public bool HasWorld(string ns, string name)
        {
            if (HasOverrideWorld(ns, name))
                return true;

            // get on other mods
            if (ModManager.Mods.Where(m => m != kernelMod && m.IsLoaded() && m.GetMetadata().Match(ns)).Any(m => m.AssetAPI.HasOverrideWorld(ns, name)))
                return true;
            
            // get from the initial mod
            var mod = ModManager.GetMod(ns);
            return mod != null && mod.AssetAPI.HasLocalAsset<Object>(name);
        }

        public Scene GetWorld(string name) => GetWorld(kernelMod.Metadata.GetId(), name);
        public Scene GetWorld(string ns, string name)
        {
            // get on override mod
            if (IsLoadedOverrideWorld(ns, name))
                return GetOverrideWorld(ns, name);

            // get on other mods
            foreach (var m in ModManager.Mods.Where(m => m != kernelMod && m.IsLoaded() && m.GetMetadata().Match(ns)).Where(m => m.AssetAPI.IsLoadedOverrideWorld(ns, name)))
                return m.AssetAPI.GetOverrideWorld(ns, name);
            
            // get from the initial mod
            var mod = ModManager.GetMod(ns);
            return mod != null ? mod.AssetAPI.GetLocalWorld(name) : default;
        }

        public async UniTask UnloadWorld(string name) => await UnloadWorld(kernelMod.Metadata.GetId(), name);
        public async UniTask UnloadWorld(string ns, string name)
        {
            // unload on override mod
            if (IsLoadedOverrideWorld(ns, name))
            {
                await UnloadOverrideWorld(ns, name);
                return;
            }

            // unload on other mods
            foreach (var m in ModManager.Mods.Where(m => m != kernelMod && m.IsLoaded() && m.GetMetadata().Match(ns)).Where(m => m.AssetAPI.IsLoadedOverrideWorld(ns, name)))
            {
                await m.AssetAPI.UnloadOverrideWorld(ns, name);
                return;
            }

            // unload from the initial mod
            var mod = ModManager.GetMod(ns);
            if (mod != null)
                await mod.AssetAPI.UnloadLocalWorld(name);
        }

        public bool IsLoadedWorld(string name) => IsLoadedWorld(kernelMod.Metadata.GetId(), name);
        public bool IsLoadedWorld(string ns, string name)
        {
            if (IsLoadedOverrideWorld(ns, name))
                return true;

            // get on other mods
            if (ModManager.Mods.Where(m => m != kernelMod && m.IsLoaded() && m.GetMetadata().Match(ns)).Any(m => m.AssetAPI.IsLoadedOverrideWorld(ns, name)))
                return true;
            
            // get from the initial mod
            var mod = ModManager.GetMod(ns);
            return mod != null && mod.AssetAPI.IsLoadedLocalWorld(name);
        }

        public bool HasOverrideAsset<T>(string ns, string name) where T : Object
        {
            List<string> namespaces = new() { ns };
            var mod = ModManager.GetMod(ns);
            if (mod != null)
            {
                var meta = mod.GetMetadata();
                namespaces.Add(meta.GetId());
                namespaces.AddRange(meta.GetProvides());
            }

            foreach (var n in namespaces)
            {
#if UNITY_EDITOR
                var dirpath = Path.Combine("Assets", Path.GetRelativePath(
                    Application.dataPath,
                    Path.Combine(kernelMod.GetData<string>("assets"), n, name))
                );

                return File.Exists(dirpath);
#else
                return !string.IsNullOrEmpty(HasAssetFromBundle(ns, name));
#endif
            }

            return false;
        }
        public T GetOverrideAsset<T>(string ns, string name) where T : Object
        {
            List<string> namespaces = new() { ns };
            var mod = ModManager.GetMod(ns);
            if (mod != null)
            {
                var meta = mod.GetMetadata();
                namespaces.Add(meta.GetId());
                namespaces.AddRange(meta.GetProvides());
            }

            foreach (var n in namespaces)
            {
#if UNITY_EDITOR
                var dirpath = Path.Combine("Assets", Path.GetRelativePath(
                    Application.dataPath,
                    Path.Combine(kernelMod.GetData<string>("assets"), n, name))
                );

                return !File.Exists(dirpath) ? default : UnityEditor.AssetDatabase.LoadAssetAtPath<T>(dirpath);
#else
                return GetAssetFromBundle<T>(ns, name);
#endif
            }

            return default;
        }

        public bool HasLocalAsset<T>(string name) where T : Object => HasOverrideAsset<T>(kernelMod.Metadata.GetId(), name);
        public T GetLocalAsset<T>(string name) where T : Object => GetOverrideAsset<T>(kernelMod.Metadata.GetId(), name);

        public async UniTask<Scene> LoadLocalWorld(string name, LoadSceneMode mode = LoadSceneMode.Single) 
            => await LoadOverrideWorld(kernelMod.Metadata.GetId(), name, mode);
        
        public async UniTask<Scene> LoadOverrideWorld(string ns, string name, LoadSceneMode mode = LoadSceneMode.Single)
        {
            Logger.LogDebug("Kernel Loading world: " + ns + "/" + name);
            List<string> namespaces = new() { ns };
            var mod = ModManager.GetMod(ns);
            if (mod != null)
            {
                var meta = mod.GetMetadata();
                namespaces.Add(meta.GetId());
                namespaces.AddRange(meta.GetProvides());
            }

            foreach (var n in namespaces)
            {
#if UNITY_EDITOR

                var dirpath = Path.Combine("Assets", Path.GetRelativePath(
                    Application.dataPath,
                    Path.Combine(kernelMod.GetData<string>("assets"), ns, name))
                );

                for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    var sc = SceneUtility.GetScenePathByBuildIndex(i);
                    if (sc.Replace('\\', '/').ToLower() == dirpath.Replace('\\', '/').ToLower())
                    {
                        var id = SceneUtility.GetBuildIndexByScenePath(sc);
                        if (id == -1) return default;
                        var scene = SceneManager.GetSceneByBuildIndex(id);
                        if (!scene.isLoaded) await SceneManager.LoadSceneAsync(id, mode);
                        scene = SceneManager.GetSceneByBuildIndex(id);
                        Logger.LogDebug("Kernel Loaded world: " + ns + "/" + name + " - " + dirpath + " - " +
                                        scene.isLoaded + " " + scene.IsValid());
                        return scene;
                    }
                }

                return default;
#else
                return await LoadWorldFromBundle(ns, name, mode);
#endif
            }

            return default;
        }

        public async UniTask UnloadLocalWorld(string name) => await UnloadOverrideWorld(kernelMod.Metadata.GetId(), name);
        public async UniTask UnloadOverrideWorld(string ns, string name)
        {
            List<string> namespaces = new() { ns };
            var mod = ModManager.GetMod(ns);
            if (mod != null)
            {
                var meta = mod.GetMetadata();
                namespaces.Add(meta.GetId());
                namespaces.AddRange(meta.GetProvides());
            }

            foreach (var n in namespaces)
            {
#if UNITY_EDITOR
                var dirpath = Path.Combine("Assets", Path.GetRelativePath(
                    Application.dataPath,
                    Path.Combine(kernelMod.GetData<string>("assets"), ns, name))
                );

                for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    var sc = SceneUtility.GetScenePathByBuildIndex(i);
                    if (sc.Replace('\\', '/').ToLower() == dirpath.Replace('\\', '/').ToLower())
                    {
                        var id = SceneUtility.GetBuildIndexByScenePath(sc);
                        if (id == -1) return;
                        var scene = SceneManager.GetSceneByBuildIndex(id);
                        if (scene.isLoaded) await SceneManager.UnloadSceneAsync(scene);
                        return;
                    }
                }
#else
                await UnloadWorldFromBundle(ns, name);
#endif
            }

            return;
        }

        public bool HasOverrideWorld(string ns, string name)
        {
            List<string> namespaces = new() { ns };
            var mod = ModManager.GetMod(ns);
            if (mod != null)
            {
                var meta = mod.GetMetadata();
                namespaces.Add(meta.GetId());
                namespaces.AddRange(meta.GetProvides());
            }

            foreach (var n in namespaces)
            {
#if UNITY_EDITOR
                var dirpath = Path.Combine("Assets", Path.GetRelativePath(
                    Application.dataPath,
                    Path.Combine(kernelMod.GetData<string>("assets"), ns, name))
                );

                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    var sc = SceneUtility.GetScenePathByBuildIndex(i);
                    if (sc.Replace('\\', '/').ToLower() == dirpath.Replace('\\', '/').ToLower())
                        return true;
                }
#else
                return !string.IsNullOrEmpty(HasWorldFromBundle(ns, name));
#endif
            }

            return false;
        }

        public bool IsLoadedOverrideWorld(string ns, string name)
        {
            List<string> namespaces = new() { ns };
            var mod = ModManager.GetMod(ns);
            if (mod != null)
            {
                var meta = mod.GetMetadata();
                namespaces.Add(meta.GetId());
                namespaces.AddRange(meta.GetProvides());
            }

            foreach (var n in namespaces)
            {
#if UNITY_EDITOR
                var dirpath = Path.Combine("Assets", Path.GetRelativePath(
                    Application.dataPath,
                    Path.Combine(kernelMod.GetData<string>("assets"), ns, name))
                );

                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    var sc = SceneUtility.GetScenePathByBuildIndex(i);
                    if (sc.Replace('\\', '/').ToLower() == dirpath.Replace('\\', '/').ToLower())
                    {
                        var id = SceneUtility.GetBuildIndexByScenePath(sc);
                        if (id == -1) return false;
                        var scene = SceneManager.GetSceneByBuildIndex(id);
                        Logger.LogDebug("Scene loaded: " + ns + "/" + name + " - " + dirpath + " - " + scene.isLoaded);
                        return scene.isLoaded;
                    }
                }
#else
                return IsLoadedWorldFromBundle(ns, name);
#endif
            }

            return false;
        }

        public Scene GetOverrideWorld(string ns, string name)
        {
            List<string> namespaces = new() { ns };
            var mod = ModManager.GetMod(ns);
            if (mod != null)
            {
                var meta = mod.GetMetadata();
                namespaces.Add(meta.GetId());
                namespaces.AddRange(meta.GetProvides());
            }

            foreach (var n in namespaces)
            {
#if UNITY_EDITOR
                var dirpath = Path.Combine("Assets", Path.GetRelativePath(
                    Application.dataPath,
                    Path.Combine(kernelMod.GetData<string>("assets"), n, name))
                );
                
                if (!File.Exists(dirpath))
                    return default;

                return SceneManager.GetSceneByPath(dirpath);
#else
                return GetWorldFromBundle(ns, name);
#endif
            }

            return default;
        }

        public bool HasLocalWorld(string name) => HasOverrideWorld(kernelMod.Metadata.GetId(), name);
        public bool IsLoadedLocalWorld(string name) => IsLoadedOverrideWorld(kernelMod.Metadata.GetId(), name);
        public Scene GetLocalWorld(string name) => GetOverrideWorld(kernelMod.Metadata.GetId(), name);




        public List<AssetBundle> assetBundles = null;

#if UNITY_EDITOR
#pragma warning disable 1998
        private bool _loaded = false;

        public async UniTask<bool> RegisterAssets()
        {
            _loaded = true;
            return true;
        }

        public async UniTask<bool> UnRegisterAssets()
        {
            _loaded = false;
            return true;
        }

        public bool IsLoaded() => _loaded;


#pragma warning restore 1998
#else
        public string GetAssetPathFromBundle(string ns, string name)
        {
            var basepath = kernelMod.Metadata.GetCustom<JObject>("kernel")?.GetValue("assets_path")?.ToObject<string>();
            if (string.IsNullOrEmpty(basepath))
            {
                Logger.LogWarning("Asset base path not found for " + kernelMod.Metadata.GetId());
                return null;
            }

            return Path.Combine(basepath, ns, name).Replace('\\', '/').ToLower();
        }

        public T GetAssetFromBundle<T>(string ns, string name) where T : Object
        {
            if (assetBundles == null)
            {
                Logger.LogWarning("Asset bundles not loaded for " + kernelMod.Metadata.GetId() + " - " + ns + "/" + name);
                return default;
            }

            var v = HasAssetFromBundle(ns, name);
            if (v == null)
                return default;
            foreach (var n in assetBundles)
                if (n.Contains(v))
                    return n.LoadAsset<T>(v);
            return default;
        }

        public string HasAssetFromBundle(string ns, string name)
        {
            if (assetBundles == null)
            {
                Logger.LogWarning("Asset bundles not loaded for " + kernelMod.Metadata.GetId() + " - " + ns + "/" + name);
                return null;
            }

            var v = GetAssetPathFromBundle(ns, name);
            foreach (var n in assetBundles)
                foreach (var a in n.GetAllAssetNames())
                    if (a.Replace('\\', '/').ToLower() == v)
                        return a;

            return null;
        }

        public async UniTask<bool> RegisterAssets()
        {
            if (assetBundles != null)
            {
                Logger.LogWarning("Asset bundles already loaded for " + kernelMod.Metadata.GetId());
                return true;
            }

            var kernel = kernelMod.Metadata.GetCustom<JObject>("kernel");
            if (kernel == null)
            {
                Logger.LogWarning("Kernel data not found for " + kernelMod.Metadata.GetId());
                return false;
            }

            var assets = kernel.GetValue("assets")?.ToObject<JObject[]>();
            if (assets == null)
            {
                Logger.LogWarning("Assets not found for " + kernelMod.Metadata.GetId());
                return false;
            }

            assetBundles = new List<AssetBundle>();

            foreach (var asset in assets)
            {
                var path = asset.GetValue("file")?.ToObject<string>();
                if (string.IsNullOrEmpty(path))
                {
                    Logger.LogWarning($"Asset bundle path not found for {kernelMod.Metadata.GetId()}");
                    continue;
                }

                path = Path.Combine(Application.dataPath, "Nox", path);
                if (!File.Exists(path))
                {
                    Logger.LogWarning($"Asset bundle {path} not found for {kernelMod.Metadata.GetId()}");
                    continue;
                }

                var bundle = await AssetBundle.LoadFromFileAsync(path);
                if (bundle == null)
                {
                    Logger.LogWarning($"Failed to load asset bundle {path} for {kernelMod.Metadata.GetId()}");
                    continue;
                }

                assetBundles.Add(bundle);
            }

            return true;
        }

        public async UniTask<bool> UnRegisterAssets()
        {
            if (assetBundles == null)
                return true;
            foreach (var bundle in assetBundles)
                await bundle.UnloadAsync(true);
            assetBundles = null;
            return true;
        }

        public bool IsLoaded() => assetBundles != null;

        public async UniTask<Scene> LoadWorldFromBundle(string ns, string name, LoadSceneMode mode = LoadSceneMode.Single)
        {
            var path = GetAssetPathFromBundle(ns, name);
            Logger.Log("Loading world: " + ns + "/" + name + " - " + path);
            if (string.IsNullOrEmpty(path))
            {
                Logger.LogWarning("Asset path not found for " + kernelMod.Metadata.GetId() + " - " + ns + "/" + name);
                return default;
            }

            foreach (var n in assetBundles)
                foreach (var a in n.GetAllScenePaths())
                    if (a.Replace('\\', '/').ToLower() == path)
                    {
                        var scene = SceneManager.GetSceneByPath(a);
                        if (!scene.isLoaded) await SceneManager.LoadSceneAsync(a, mode);
                        return scene;
                    }

            Logger.LogWarning("World not found: " + ns + "/" + name);

            return default;
        }

        public async UniTask UnloadWorldFromBundle(string ns, string name)
        {
            var path = GetAssetPathFromBundle(ns, name);
            Logger.Log("Unloading world: " + ns + "/" + name + " - " + path);
            if (string.IsNullOrEmpty(path))
            {
                Logger.LogWarning("Asset path not found for " + kernelMod.Metadata.GetId() + " - " + ns + "/" + name);
                return;
            }

            foreach (var n in assetBundles)
                foreach (var a in n.GetAllScenePaths())
                    if (a.Replace('\\', '/').ToLower() == path)
                    {
                        var scene = SceneManager.GetSceneByPath(a);
                        if (scene.isLoaded) await SceneManager.UnloadSceneAsync(scene);
                        return;
                    }

            Logger.LogWarning("World not found: " + ns + "/" + name);
        }

        public string HasWorldFromBundle(string ns, string name)
        {
            var path = GetAssetPathFromBundle(ns, name);
            Logger.Log("Checking HAS world: " + ns + "/" + name + " - " + path);
            if (string.IsNullOrEmpty(path))
            {
                Logger.LogWarning("Asset path not found for " + kernelMod.Metadata.GetId() + " - " + ns + "/" + name);
                return null;
            }

            foreach (var n in assetBundles)
                foreach (var a in n.GetAllScenePaths())
                    if (a.Replace('\\', '/').ToLower() == path)
                        return a;

            return null;
        }

        public bool IsLoadedWorldFromBundle(string ns, string name)
        {
            var path = GetAssetPathFromBundle(ns, name);
            Logger.Log("Checking LOADED world: " + ns + "/" + name + " - " + path);
            if (string.IsNullOrEmpty(path))
            {
                Logger.LogWarning("Asset path not found for " + kernelMod.Metadata.GetId() + " - " + ns + "/" + name);
                return false;
            }

            foreach (var n in assetBundles)
                foreach (var a in n.GetAllScenePaths())
                    if (a.Replace('\\', '/').ToLower() == path)
                    {
                        var scene = SceneManager.GetSceneByPath(a);
                        Logger.Log("Scene loaded: " + ns + "/" + name + " - " + path + " - " + scene.isLoaded);
                        return scene.isLoaded;
                    }

            Logger.LogWarning("aa World not found: " + ns + "/" + name);
            return false;
        }

        public Scene GetWorldFromBundle(string ns, string name)
        {
            var path = GetAssetPathFromBundle(ns, name);
            Logger.Log("Getting world: " + ns + "/" + name + " - " + path);
            if (string.IsNullOrEmpty(path))
            {
                Logger.LogWarning("Asset path not found for " + kernelMod.Metadata.GetId() + " - " + ns + "/" + name);
                return default;
            }

            foreach (var n in assetBundles)
                foreach (var a in n.GetAllScenePaths())
                    if (a.Replace('\\', '/').ToLower() == path)
                        return SceneManager.GetSceneByPath(a);

            Logger.LogWarning("World not found: " + ns + "/" + name);

            return default;
        }
#endif

    }
}