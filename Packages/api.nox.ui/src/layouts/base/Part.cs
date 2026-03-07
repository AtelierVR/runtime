using System.Linq;
using api.nox.ui.menus;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.UI;
using UnityEngine;

namespace api.nox.ui.layouts {
	public abstract class Part : MonoBehaviour, IPart {
		public abstract string              GetKey();
		public abstract UniTask<GameObject> GetPrefab();
		public          RectTransform       container;
		public          Menu                menu;


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
				if (el.GetData().Key != key) continue;
				#if UNITY_EDITOR
				if (Application.isPlaying) Destroy(el.gameObject);
				else UnityEditor.EditorApplication.delayCall += () => DestroyImmediate(el.gameObject);
				#else
				Destroy(el.gameObject);
				#endif
			}
		}

		public UniTask AddElement(NavigationData element)
			=> AddElement(element, null);

		public virtual async UniTask AddElement(NavigationData data, GameObject prefab = null) {
			var elementComponent = GetChildren().FirstOrDefault(e => e.GetData().Key == data.Key);

			if (!elementComponent) {
				prefab = await GetPrefab();
				var instance = Instantiate(prefab, container);
				elementComponent = instance.GetComponent<Element>();

				if (!elementComponent) {
					Debug.LogError($"Prefab {prefab.name} does not have Element component. Cannot add element to part {name}.");
					instance.Destroy();
					return;
				}

				instance.name = elementComponent.GetInstanceID().ToString("x8");
			}

			await elementComponent.SetData(menu, data);
			UpdateLayout.UpdateImmediate(container);
		}

		public virtual async UniTask AddElements(NavigationData[] elements) {
			var prefab = await GetPrefab();
			await UniTask.WhenAll(elements.Select(e => AddElement(e, prefab)));
			UpdateLayout.UpdateImmediate(container);
		}
	}
}