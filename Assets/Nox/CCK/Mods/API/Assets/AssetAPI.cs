using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nox.CCK.Mods.Assets
{
    public interface AssetAPI
    {
        public bool HasAsset<T>(string ns, string name) where T : Object; // check override local assets first, then override assets, then local assets
        public T GetAsset<T>(string ns, string name) where T : Object; // check override local assets first, then override assets, then local assets

        public bool HasAsset<T>(string name) where T : Object; // check override local assets first, then override assets, then local assets
        public T GetAsset<T>(string name) where T : Object; // check override local assets first, then override assets, then local assets

        public bool HasLocalAsset<T>(string name) where T : Object; // check strictly local assets
        public T GetLocalAsset<T>(string name) where T : Object; // check strictly local assets

        public bool HasOverrideAsset<T>(string ns, string name) where T : Object; // check override local assets first
        public T GetOverrideAsset<T>(string ns, string name) where T : Object; // check override local assets first


        public bool HasWorld(string ns, string name);
        public bool IsLoadedWorld(string ns, string name);
        public Scene GetWorld(string ns, string name);
        public UniTask<Scene> LoadWorld(string ns, string name, LoadSceneMode mode = LoadSceneMode.Single);
        public UniTask UnloadWorld(string ns, string name);

        public bool HasWorld(string name);
        public bool IsLoadedWorld(string name);
        public Scene GetWorld(string name);
        public UniTask<Scene> LoadWorld(string name, LoadSceneMode mode = LoadSceneMode.Single);
        public UniTask UnloadWorld(string name);

        public bool HasLocalWorld(string name);
        public bool IsLoadedLocalWorld(string name);
        public Scene GetLocalWorld(string name);
        public UniTask<Scene> LoadLocalWorld(string name, LoadSceneMode mode = LoadSceneMode.Single);
        public UniTask UnloadLocalWorld(string name);

        public bool HasOverrideWorld(string ns, string name);
        public bool IsLoadedOverrideWorld(string ns, string name);
        public Scene GetOverrideWorld(string ns, string name);
        public UniTask<Scene> LoadOverrideWorld(string ns, string name, LoadSceneMode mode = LoadSceneMode.Single);
        public UniTask UnloadOverrideWorld(string ns, string name);
    }
}