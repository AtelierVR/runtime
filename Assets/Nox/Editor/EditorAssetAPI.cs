
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nox.Editor.Mods
{
    public class EditorAssetAPI : CCK.Mods.Assets.AssetAPI
    {
        private EditorMod _mod;
        internal EditorAssetAPI(EditorMod mod) => _mod = mod;
        public T GetAsset<T>(string ns, string name) where T : Object
        {
            foreach (var path in EditorModManager.GetResourcesPaths())
            {
                var dir = Path.Combine(Application.dataPath, path, ns);
                if (!Directory.Exists(dir)) continue;
                var files = Directory.GetFiles(dir, $"{name}.*", SearchOption.AllDirectories);
                if (files.Length > 0)
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<T>("Assets/" + files[0][(Application.dataPath.Length + 1)..]);
            }
            return null;
        }
        public bool HasAsset<T>(string ns, string name) where T : Object
        {
            foreach (var path in EditorModManager.GetResourcesPaths())
            {
                var dir = Path.Combine(Application.dataPath, path, ns);
                if (!Directory.Exists(dir)) continue;
                if (Directory.GetFiles(dir, $"{name}.*", SearchOption.AllDirectories).Length > 0)
                    return true;
            }
            return false;
        }
        public T GetLocalAsset<T>(string name) where T : Object => GetAsset<T>(_mod.GetMetadata().GetId(), name);
        public bool HasLocalAsset<T>(string name) where T : Object => HasAsset<T>(_mod.GetMetadata().GetId(), name);

        public Scene LoadWorld(string ns, string name, LoadSceneMode mode = LoadSceneMode.Single)
        {
            throw new System.NotImplementedException();
        }

        public Scene LoadLocalWorld(string name, LoadSceneMode mode = LoadSceneMode.Single)
        {
            throw new System.NotImplementedException();
        }
    }
}