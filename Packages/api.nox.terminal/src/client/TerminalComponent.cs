using Nox.CCK.Utils;
using UnityEngine;

namespace api.nox.terminal.client {
	public class TerminalComponent : MonoBehaviour {
		private TerminalPage _page;
		private GameObject   _container;
		private GameObject   _navigation;

		public static (GameObject, TerminalComponent) Generate(TerminalPage page, RectTransform parent) {
			var content = Instantiate(Client.GetAsset<GameObject>("prefabs/split.prefab", "ui"), parent);

			var component = content.AddComponent<TerminalComponent>();
			component._page = page;
			content.name   = $"[{page.GetKey()}_{content.GetInstanceID()}]";
			var splitContent = Reference.GetComponent<RectTransform>("content", content);

			// generate dashboard
			component._container = Instantiate(Client.GetAsset<GameObject>("prefabs/container_full.prefab", "ui"), splitContent);

			// generate profile
			component._navigation = Instantiate(Client.GetAsset<GameObject>("prefabs/container.prefab", "ui"), splitContent);

			return (content, component);
		}
	}
}