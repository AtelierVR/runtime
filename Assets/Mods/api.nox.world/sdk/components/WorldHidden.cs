using System.Linq;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nox.Worlds.Components {
	[Nox.CCK.Development.Gizmos("world.hidden")]
	public class WorldHidden : MonoBehaviour, Nox.CCK.Development.IGizmos, INoxObject {
		/// <summary>
		/// Returns whether the GameObject is a valid root GameObject for a world.
		/// </summary>
		[NoxPublic(NoxAccess.Method)]
		public bool IsValid() {
			if (!gameObject.scene.isLoaded)
				return false;
			var roots = gameObject.scene.GetRootGameObjects();
			return roots.Any(root => root == gameObject);
		}

		/// <summary>
		/// Returns whether the GameObject is hidden.
		/// </summary>
		/// <returns></returns>
		[NoxPublic(NoxAccess.Method)]
		public bool IsHidden()
			=> !gameObject.activeSelf || !gameObject.activeInHierarchy;

		/// <summary>
		/// Sets the GameObject to be hidden or not.
		/// </summary>
		[NoxPublic(NoxAccess.Method)]
		public void Set(bool active)
			=> gameObject.SetActive(active);

		/// <summary>
		/// Returns the WorldHidden component of the scene.
		/// </summary>
		public static WorldHidden Get(Scene scene)
			=> TryGet(scene, out var worldHidden) ? worldHidden : null;

		/// <summary>
		/// Returns the WorldHidden component of the scene.
		/// </summary>
		/// <param name="scene"></param>
		/// <param name="worldHidden"></param>
		/// <returns></returns>
		public static bool TryGet(Scene scene, out WorldHidden worldHidden) {
			if (!scene.IsValid()) {
				worldHidden = null;
				return false;
			}

			foreach (var root in scene.GetRootGameObjects())
				if (root.TryGetComponent(out worldHidden))
					return true;

			worldHidden = null;
			return false;
		}

		public static bool Has(Scene scene)
			=> Get(scene);

		public static WorldHidden Make(Scene scene) {
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

		[NoxPublic(NoxAccess.Method)]
		public void OnDrawGizmos() {
			if (IsValid()) return;
			CCK.Development.Gizmos.color = Color.red;
			CCK.Development.Gizmos.DrawWireCube(transform.position, new Vector3(1, 1, 1));
		}
	}
}