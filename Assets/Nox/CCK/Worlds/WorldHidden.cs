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
        public void SetHidden(bool hidden) => gameObject.SetActive(!hidden);

        /// <summary>
        /// Returns the WorldHidden component of the scene.
        /// </summary>
        public static WorldHidden GetWorldHidden(Scene scene)
        {
            if (!scene.IsValid()) return null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.TryGetComponent<WorldHidden>(out var worldHidden))
                    return worldHidden;
            return null;
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
                Debug.LogWarning("WorldHidden must be the root GameObject of a scene.");
        }

        public void OnValidate()
        {
            if (!IsValid())
                Debug.LogWarning("WorldHidden must be the root GameObject of a scene.");
        }
#endif
    }
}