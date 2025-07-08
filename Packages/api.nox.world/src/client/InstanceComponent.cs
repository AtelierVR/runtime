using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Instances;
using UnityEngine;
using UnityEngine.UI;
using Transform = UnityEngine.Transform;

namespace api.nox.world.client {
	public class InstanceComponent : MonoBehaviour {
		public static (GameObject go, InstanceComponent comp) Generate(WorldComponent worldComponent, Transform parent) {
			var instance  = Instantiate(Client.GetAsset<GameObject>("prefabs/instance.prefab", "instance"), parent);
			var component = instance.AddComponent<InstanceComponent>();
			component.world = worldComponent;
			component.label = Reference.GetComponent<TextLanguage>("label", instance);
			component.text  = Reference.GetComponent<TextLanguage>("text", instance);
			component.image = Reference.GetComponent<Image>("image", instance);
			return (instance, component);
		}

		public  WorldComponent          world;
		public  TextLanguage            label;
		public  TextLanguage            text;
		public  Image                   image;
		private CancellationTokenSource _thumbnailTokenSource;

		public void UpdateContent(IInstance instance) {
			label.UpdateText("world.instance.label", new[] { instance.GetName() });
			text.UpdateText("world.instance.text", new[] { instance.GetTitle() ?? world.Page.World.GetTitle() ?? instance.ToIdentifier().ToString() });
			UpdateThumbnail(instance).Forget();
		}


		private async UniTask UpdateThumbnail(IInstance instance) {
			if (_thumbnailTokenSource != null) {
				_thumbnailTokenSource?.Cancel();
				_thumbnailTokenSource?.Dispose();
			}

			_thumbnailTokenSource = new CancellationTokenSource();
			var url = instance?.GetThumbnailUrl() ?? world.Page.World.GetThumbnailUrl();

			if (!string.IsNullOrEmpty(url)) {
				var texture = await Main.Instance.NetworkAPI
					.FetchTexture(url)
					.AttachExternalCancellation(_thumbnailTokenSource.Token);
				image.sprite = texture
					? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero)
					: null;
			} else image.sprite = null;

			_thumbnailTokenSource = null;
		}
	}
}