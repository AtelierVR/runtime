using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nox.CCK.Utils
{
    public class Finder
    {
        public static GameObject Find(string name, GameObject parent)
        {
            foreach (UnityEngine.Transform child in parent.transform)
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
            return scene.GetRootGameObjects()
                .Select(gameObject => Find(name, gameObject))
                .FirstOrDefault(result => result);
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

        public static T FindComponent<T>(GameObject parent)
        {
            if (parent.TryGetComponent<T>(out var component))
                return component;
            foreach (UnityEngine.Transform child in parent.transform)
            {
                component = FindComponent<T>(child.gameObject);
                if (component != null)
                    return component;
            }
            return default;
        }

        public static T FindComponent<T>(Scene scene = default)
        {
            if (scene == default)
                scene = SceneManager.GetActiveScene();
            foreach (var gameObject in scene.GetRootGameObjects())
            {
                var component = FindComponent<T>(gameObject);
                if (component != null)
                    return component;
            }
            return default;
        }
    }
}