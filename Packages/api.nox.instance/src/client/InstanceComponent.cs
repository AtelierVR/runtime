using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Instances;
using Nox.Worlds;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.instance.client {
	public class InstanceComponent : MonoBehaviour {
		public InstancePage Page;

		public static (GameObject, InstanceComponent) Generate(InstancePage page, RectTransform parent) {
			var content        = Instantiate(Client.GetAsset<GameObject>("prefabs/split.prefab", "ui"), parent);
			var withTitleAsset = Client.GetAsset<GameObject>("prefabs/with_title.prefab", "ui");

			var component = content.AddComponent<InstanceComponent>();
			component.Page = page;
			content.name   = $"[{page.GetKey()}_{content.GetInstanceID()}]";

			var splitContent   = Reference.GetComponent<RectTransform>("content", content);
			var containerAsset = Client.GetAsset<GameObject>("prefabs/container.prefab", "ui");

			// generate dashboard
			var container = Instantiate(Client.GetAsset<GameObject>("prefabs/container_full.prefab", "ui"), splitContent);
			var withTitle = Instantiate(
				withTitleAsset,
				Reference.GetComponent<RectTransform>("content", container)
			);
			return (content, component);
		}
	}
}