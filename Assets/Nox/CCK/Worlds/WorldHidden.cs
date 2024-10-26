using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nox.CCK.Worlds
{
    public class WorldHidden : MonoBehaviour
    {

        /// <summary>
        /// Returns whether the GameObject is a valid root GameObject for a world.
        /// </summary>
        public bool IsValid()
        {
            if (!gameObject.scene.isLoaded)
                return false;
            var roots = gameObject.scene.GetRootGameObjects();
            foreach (var root in roots)
                if (root == gameObject)
                    return true;
            return false;
        }

        /// <summary>
        /// Returns whether the GameObject is hidden.
        /// </summary>
        /// <returns></returns>
        public bool IsHidden() => !gameObject.activeSelf || !gameObject.activeInHierarchy;

        /// <summary>
        /// Sets the GameObject to be hidden or not.
        /// </summary>
        public void Set(bool hidden) => gameObject.SetActive(hidden);

        /// <summary>
        /// Returns the WorldHidden component of the scene.
        /// </summary>
        public static WorldHidden Get(Scene scene)
        {
            if (!scene.IsValid()) return null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.TryGetComponent<WorldHidden>(out var worldHidden))
                    return worldHidden;
            return null;
        }

        public static bool Has(Scene scene) => Get(scene) != null;

        public static WorldHidden Make(Scene scene)
        {
            if (Has(scene))
                return Get(scene);
            var go = new GameObject("WorldHidden");
            go.transform.SetAsFirstSibling();
            var comp = go.AddComponent<WorldHidden>();
            SceneManager.MoveGameObjectToScene(go, scene);
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
                if (root != go)
                    root.transform.SetParent(go.transform);
            return comp;
        }

#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            if (!IsValid())
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position, new Vector3(1, 1, 1));
            }
        }
        public void Awake()
        {
            if (!IsValid())
                Logger.LogWarning("WorldHidden must be the root GameObject of a scene.");
        }

        public void OnValidate()
        {
            if (!IsValid())
                Logger.LogWarning("WorldHidden must be the root GameObject of a scene.");
        }
#endif
    }
}