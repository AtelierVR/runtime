#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace api.nox.world
{
    class WorldLoader
    {
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

        public static VisualElement OpenTab(bool selected = false)
        {
            if (!selected) return null;
            var root = new VisualElement();
            foreach (var file in WorldFiles())
                root.Add(new Button(() => StartAndLoadWorld(file)) { text = file });
            return root;
        }
    }
}
#endif