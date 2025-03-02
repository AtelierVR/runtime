#if UNITY_EDITOR
using System.Collections.Generic;
using Nox.CCK.Mods.Panels;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.world
{
    public class WorldLoaderPanel : EditorPanelBuilder
    {
        public string Id { get; } = "loader";
        public string Name { get; } = "World/Loader";
        public bool Hidded { get; } = false;

        internal WorldEditorMod _mod;
        internal WorldLoaderPanel(WorldEditorMod mod) => _mod = mod;

        public VisualElement OnOpenned(Dictionary<string, object> data)
        {
            var root = new VisualElement();
            foreach (var file in WorldFiles())
                root.Add(new Button(() => StartAndLoadWorld(file)) { text = file });
            return root;
        }

        public static string[] WorldFiles()
        {
            return System.IO.Directory.GetFiles("Assets", "*.noxw", System.IO.SearchOption.AllDirectories);
        }

        public static void LoadWorld(string path)
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            var scenes = bundle.GetAllScenePaths();
            // close others scenes

            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i));

            foreach (var scene in scenes)
                UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
        }

        public static void StartPlayMode()
        {
            if (!Application.isPlaying)
                UnityEditor.EditorApplication.isPlaying = true;
        }

        public static void StartAndLoadWorld(string path)
        {
            StartPlayMode();
            LoadWorld(path);
        }

        public void OnClosed()
        {
            Logger.Log("Panel Example closed!");
        }
    }
}
#endif