using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods.Panels;
using UnityEngine.UIElements;
using Nox.CCK.Utils;

namespace api.nox.world {
	public class WorldManagerPanel : IEditorPanelBuilder {
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

			var child = Main.Instance.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("manager.uxml").CloneTree();
			child.style.flexGrow = 1;
			_root.Add(child);

			_root.Q<Label>("version").text = "v" + Main.Instance.CoreAPI.ModMetadata.GetVersion();

			foreach (var world in Main.Instance.GroupManager.Groups)
				OnWorldAdded(world);

			return _root;
		}

		public WorldManagerPanel() {
			Main.Instance.GroupManager.OnGroupAdded.AddListener(OnWorldAdded);
			Main.Instance.GroupManager.OnGroupRemoved.AddListener(OnWorldRemoved);
		}

		public void Dispose() {
			Main.Instance?.GroupManager?.OnGroupAdded?.RemoveListener(OnWorldAdded);
			Main.Instance?.GroupManager?.OnGroupRemoved?.RemoveListener(OnWorldRemoved);
			_root?.ClearBindings();
			_root?.Clear();
			_root?.RemoveFromHierarchy();
		}

		private void OnWorldAdded(RuntimeWorldGroup runtimeWorldGroup) {
			var list = _root.Q("list");
			if (list == null) return;
			var child = list.Children().FirstOrDefault(c => c.userData is string id && id == runtimeWorldGroup.Id);
			if (child != null) {
				UpdateWorld(child, runtimeWorldGroup);
				return;
			}

			child                = Main.Instance.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("world.uxml").CloneTree();
			child.style.flexGrow = 1;
			child.userData       = runtimeWorldGroup.Id;
			UpdateWorld(child, runtimeWorldGroup);
			list.Add(child);
		}


		private void OnWorldRemoved(RuntimeWorldGroup runtimeWorldGroup) {
			var list = _root.Q("list");
			if (list == null) return;
			var child = _root.Children().FirstOrDefault(c => c.userData is string id && id == runtimeWorldGroup.Id);
			child?.RemoveFromHierarchy();
		}

		private void UpdateWorld(VisualElement child, RuntimeWorldGroup runtimeWorldGroup) {
			var label = child.Q<Label>("id");
			label.text = runtimeWorldGroup.Id;
		}
	}
}