using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods.Panels;
using UnityEngine.UIElements;
using Nox.CCK.Utils;

namespace api.nox.world {
	public class WorldManagerPanel : EditorPanelBuilder {
		public string GetId()
			=> "manager";

		public string GetName()
			=> "World/Manager";

		public bool IsHidden()
			=> false;

		private readonly VisualElement _root = new();

		public VisualElement Make(Dictionary<string, object> data) {
			_root.ClearBindings();
			_root.Clear();

			var child = WorldSystem.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("manager.uxml").CloneTree();
			child.style.flexGrow = 1;
			_root.Add(child);

			_root.Q<Label>("version").text = "v" + WorldSystem.CoreAPI.ModMetadata.GetVersion();

			foreach (var world in WorldSystem.Instance.Manager.Worlds)
				OnWorldAdded(world);

			return _root;
		}

		public WorldManagerPanel() {
			WorldSystem.Instance.Manager.OnWorldAdded.AddListener(OnWorldAdded);
			WorldSystem.Instance.Manager.OnWorldRemoved.AddListener(OnWorldRemoved);
		}

		public void Dispose() {
			WorldSystem.Instance.Manager.OnWorldAdded.RemoveListener(OnWorldAdded);
			WorldSystem.Instance.Manager.OnWorldRemoved.RemoveListener(OnWorldRemoved);
			_root.ClearBindings();
			_root.Clear();
			_root.RemoveFromHierarchy();
		}

		private void OnWorldAdded(BaseWorld world) {
			var list = _root.Q("list");
			if (list == null) return;
			var child = list.Children().FirstOrDefault(c => c.userData is string id && id == world.Id);
			if (child != null) {
				UpdateWorld(child, world);
				return;
			}

			child                = WorldSystem.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("world.uxml").CloneTree();
			child.style.flexGrow = 1;
			child.userData       = world.Id;
			UpdateWorld(child, world);
			list.Add(child);
		}


		private void OnWorldRemoved(BaseWorld world) {
			var list = _root.Q("list");
			if (list == null) return;
			var child = _root.Children().FirstOrDefault(c => c.userData is string id && id == world.Id);
			child?.RemoveFromHierarchy();
		}

		private void UpdateWorld(VisualElement child, BaseWorld world) {
			var label = child.Q<Label>("id");
			label.text = world.Id;
		}
	}
}