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
        => _mod.GetModType().GetAsset<T>(ns, name);

        public T GetLocalAsset<T>(string name) where T : Object
            => _mod.GetModType().GetAsset<T>(_mod.GetMetadata().GetId(), name);

        public Scene LoadLocalWorld(string name)
            => _mod.GetModType().LoadScene(_mod.GetMetadata().GetId(), name);

        public Scene LoadWorld(string ns, string name)
            => _mod.GetModType().LoadScene(ns, name);

        public bool HasAsset<T>(string ns, string name) where T : Object
        {
            throw new System.NotImplementedException();
        }

        public bool HasLocalAsset<T>(string name) where T : Object
        {
            throw new System.NotImplementedException();
        }
    }
}