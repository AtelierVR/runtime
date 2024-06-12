using UnityEngine;

namespace Nox.CCK.Mods.Assets
{
    public interface AssetAPI
    {
        public bool HasAsset<T>(string ns, string name) where T : Object;
        public T GetAsset<T>(string ns, string name) where T : Object;

        public bool HasLocalAsset<T>(string name) where T : Object;
        public T GetLocalAsset<T>(string name) where T : Object;
    }
}