using System.Linq;
using api.nox.ui.menus;
using Nox.CCK.Utils;
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

		public void AddElement(NavigationData element)
			=> AddElement(element, null);

		public virtual void AddElement(NavigationData data, GameObject prefab = null) {
			var elementComponent = GetChildren().FirstOrDefault(e => e.GetData().key == data.key);

			if (!elementComponent) {
				prefab = GetPrefab();
				var instance = Instantiate(prefab, container);
				elementComponent = instance.GetComponent<Element>();

				if (!elementComponent) {
					Debug.LogError($"Prefab {GetPrefab().name} does not have Element component. Cannot add element to part {name}.");
					instance.Destroy();
					return;
				}

				instance.name = elementComponent.GetInstanceID().ToString("x8");
			}

			elementComponent.SetData(menu, data);
		}

		public virtual void AddElements(NavigationData[] elements) {
			var prefab = GetPrefab();
			foreach (var data in elements)
				AddElement(data, prefab);
		}
	}
}