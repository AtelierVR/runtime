#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Nox.CCK.Mods.Panels;
using Nox.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace api.nox.controller {
	public class ControllerPanel : IEditorPanelBuilder, IDisposable {
		public string GetId()
			=> "controller";

		public string GetName()
			=> "Controller";

		public bool IsHidden()
			=> false;

		private readonly VisualElement _root       = new();
		private          DateTime      _lastUpdate = DateTime.MinValue;
		private          IController   _lastController;

		public void OnUpdate() {
			if (DateTime.UtcNow - _lastUpdate < TimeSpan.FromSeconds(0.5)) return;
			_lastUpdate = DateTime.UtcNow;

			var api = Main.Instance as IControllerAPI;
			var currentController = api?.GetCurrent();

			if (_lastController != currentController) {
				_lastController = currentController;
				UpdateControllerInfo();
			}
		}

		private void UpdateControllerInfo() {
			var noControllerLabel = _root.Q<Label>("no-controller");
			var controllerInfo = _root.Q<VisualElement>("controller-info");

			if (_lastController == null) {
				if (noControllerLabel != null) noControllerLabel.style.display = DisplayStyle.Flex;
				if (controllerInfo != null) controllerInfo.style.display = DisplayStyle.None;
				return;
			}

			if (noControllerLabel != null) noControllerLabel.style.display = DisplayStyle.None;
			if (controllerInfo != null) controllerInfo.style.display = DisplayStyle.Flex;

			// Update controller information
			var idLabel = _root.Q<Label>("controller-id");
			var priorityLabel = _root.Q<Label>("controller-priority");
			var cameraLabel = _root.Q<Label>("controller-camera");
			var colliderLabel = _root.Q<Label>("controller-collider");
			var positionLabel = _root.Q<Label>("controller-position");

			if (idLabel != null) 
				idLabel.text = $"ID: {_lastController.GetId() ?? "N/A"}";

			if (priorityLabel != null) 
				priorityLabel.text = $"Priority: {_lastController.GetPriority()}";

			var camera = _lastController.GetCamera();
			if (cameraLabel != null) 
				cameraLabel.text = $"Camera: {(camera != null ? camera.name : "None")}";

			var collider = _lastController.GetCollider();
			if (colliderLabel != null) 
				colliderLabel.text = $"Collider: {(collider != null ? collider.name : "None")}";

			if (positionLabel != null && camera != null) {
				var pos = camera.transform.position;
				positionLabel.text = $"Position: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})";
			} else if (positionLabel != null) 
				positionLabel.text = "Position: N/A";
		}

		public VisualElement Make(Dictionary<string, object> data) {
			_root.ClearBindings();
			_root.Clear();

			var child = Editor.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("controller-panel.uxml").CloneTree();
			child.style.flexGrow = 1;
			_root.Add(child);

			var version = _root.Q<Label>("version");
			if (version != null) 
				version.text = "v" + Editor.CoreAPI.ModMetadata.GetVersion();

			UpdateControllerInfo();

			return _root;
		}

		public void Dispose() {
			// Nothing specific to dispose for now
		}
	}
}
#endif // UNITY_EDITOR
