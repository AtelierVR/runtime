using Nox.CCK;
using Nox.CCK.Mods.Assets;
using Nox.Mods.Client;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nox.Mods.Assets
{
    public class RuntimeAssetAPI : AssetAPI
    {
        private RuntimeMod _mod;
        internal RuntimeAssetAPI(RuntimeMod mod)
        {
            _mod = mod;
        }

        public T GetAsset<T>(string ns, string name) where T : Object
        {
            var mod = _mod.coreAPI.RuntimeModAPI.GetInternalMod(ns);
            if (mod == null) return null;
            return _mod.GetModType().GetAsset<T>(mod.GetMetadata().GetId(), name);
        }

        public T GetLocalAsset<T>(string name) where T : Object
            => _mod.GetModType().GetAsset<T>(_mod.GetMetadata().GetId(), name);

        public bool HasAsset<T>(string ns, string name) where T : Object
        {
            throw new System.NotImplementedException();
        }

        public bool HasLocalAsset<T>(string name) where T : Object
        {
            throw new System.NotImplementedException();
        }

        public Scene LoadLocalWorld(string name, LoadSceneMode mode = LoadSceneMode.Single)
            => _mod.GetModType().LoadScene(_mod.GetMetadata().GetId(), name, mode);

        public Scene LoadWorld(string ns, string name, LoadSceneMode mode = LoadSceneMode.Single)
            => _mod.GetModType().LoadScene(ns, name, mode);
    }
}