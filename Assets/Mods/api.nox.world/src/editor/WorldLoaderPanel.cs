#if UNITY_EDITOR
using System.Collections.Generic;
using Nox.CCK.Mods.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace api.nox.world
{
    public class WorldLoaderPanel : EditorPanelBuilder
    {
        public string GetId() => "loader";
        public string GetName() => "World/Loader";
        public bool IsHidden() => false;

        public VisualElement OnOpened(Dictionary<string, object> data)
        {
            var root = new VisualElement();
            foreach (var file in WorldFiles())
                root.Add(new Button(() => StartAndLoadWorld(file)) { text = file });
            return root;
        }

        private static string[] WorldFiles() 
            => System.IO.Directory.GetFiles("Assets", "*.noxw", System.IO.SearchOption.AllDirectories);

        private static void LoadWorld(string path)
        {
            var bundle = AssetBundle.LoadFromFile(path);
            var scenes = bundle.GetAllScenePaths();
            // close others scenes

            for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i));

            foreach (var scene in scenes)
                UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
        }

        private static void StartPlayMode()
        {
            if (!Application.isPlaying)
                UnityEditor.EditorApplication.isPlaying = true;
        }

        private static void StartAndLoadWorld(string path)
        {
            StartPlayMode();
            LoadWorld(path);
        }
    }
}
#endif