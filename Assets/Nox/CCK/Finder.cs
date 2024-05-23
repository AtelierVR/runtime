using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nox.CCK
{
    public class Finder
    {
        public static GameObject Find(string name, GameObject parent)
        {
            foreach (Transform child in parent.transform)
            {
                if (child.name == name)
                    return child.gameObject;
                var result = Find(name, child.gameObject);
                if (result != null)
                    return result;
            }
            return null;
        }

        public static GameObject Find(string name, Scene scene = default)
        {
            if (scene == default)
                scene = SceneManager.GetActiveScene();
            foreach (var gameObject in scene.GetRootGameObjects())
            {
                var result = Find(name, gameObject);
                if (result != null)
                    return result;
            }
            return null;
        }

        public static T FindComponent<T>(string gameObjectName, Scene scene = default)
        {
            var go = Find(gameObjectName, scene);
            if (go == null) return default;
            return go.GetComponent<T>();
        }

        public static T FindComponent<T>(string gameObjectName, GameObject parent)
        {
            var go = Find(gameObjectName, parent);
            if (go == null) return default;
            return go.GetComponent<T>();
        }
    }
}