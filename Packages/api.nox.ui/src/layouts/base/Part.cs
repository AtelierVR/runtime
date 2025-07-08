using System.Linq;
using api.nox.ui.menus;
using Nox.CCK.Mods.Cores;
using Nox.UI;
using UnityEngine;

namespace api.nox.ui.layouts {
	public abstract class Part : MonoBehaviour, IPart {
		public abstract string        GetKey();
		public abstract GameObject    GetPrefab();
		public          RectTransform container;
		public          Menu          menu;
		

		public bool GetActive()
			=> gameObject.activeSelf;

		public void SetActive(bool active)
			=> gameObject.SetActive(active);

		public Element[] GetChildren()
			=> GetComponentsInChildren<Element>();

		public NavigationData[] GetElements()
			=> GetChildren()
				.Select(e => e.GetData())
				.ToArray();

		public virtual void RemoveElement(string key) {
			foreach (var el in GetChildren()) {
				if (el.GetData().key != key) continue;
				#if UNITY_EDITOR
				if (Application.isPlaying) Destroy(el.gameObject);
				else UnityEditor.EditorApplication.delayCall += () => DestroyImmediate(el.gameObject);
				#else
				Destroy(el.gameObject);
				#endif
			}
		}

		public virtual void AddElement(NavigationData data) {
			var elementComponent = GetChildren().FirstOrDefault(e => e.GetData().key == data.key);

			if (!elementComponent) {
				var instance = Instantiate(GetPrefab(), container);
				elementComponent = instance.GetComponent<Element>();

				if (!elementComponent) {
					Debug.LogError($"Prefab {GetPrefab().name} does not have Element component. Cannot add element to part {name}.");
					#if UNITY_EDITOR
					if (Application.isPlaying) Destroy(instance);
					else UnityEditor.EditorApplication.delayCall += () => DestroyImmediate(instance);
					#else
				Destroy(instance);
					#endif
					return;
				}

				instance.name = elementComponent.GetInstanceID().ToString("x8");
			}

			elementComponent.SetData(menu, data);
		}
	}
}