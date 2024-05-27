#if UNITY_EDITOR
using System.Collections.Generic;
using Nox.CCK.Editor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace api.nox.world
{
    public class WorldLoaderPanel : EditorPanelBuilder
    {
        public string Id { get; } = "loader";
        public string Name { get; } = "World/Loader";

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

            for (int i = 0; i < SceneManager.sceneCount; i++)
                SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i));

            foreach (var scene in scenes)
                SceneManager.LoadScene(scene);
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
            Debug.Log("Panel Example closed!");
        }
    }
}
#endif