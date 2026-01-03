using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Controllers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.controller {
	public class Main : IControllerAPI, IMainModInitializer {
		public static Main Instance { get; private set; }

		private IController     _current;
		private IMainModCoreAPI _coreAPI;
		private IController     _current1;

		public void OnInitializeMain(IMainModCoreAPI api) {
			Instance = this;
			_coreAPI = api;
			_current = null;
		}

		public async UniTask OnDisposeMainAsync() {
			await SetCurrent(null);
			_coreAPI = null;
			Instance = null;
		}

		public IController Current
			=> _current;


		public UnityEvent<IController> OnCurrentChanged { get; } = new();

		private void NotifyCurrentChanged(IController controller) {
			_coreAPI?.EventAPI.Emit("controller_changed", null);
			OnCurrentChanged?.Invoke(controller);
		}

		public async UniTask<bool> SetCurrent(IController controller) {
			if (_current == controller)
				return true;

			var canChange = true;
			_coreAPI.EventAPI.Emit("controller_request_change", controller, new Action<object[]>(OnRequest));
			if (!canChange) {
				Logger.LogWarning("Controller change request was denied");
				return false;
			}

			if (controller == null) {
				if (_current == null)
					return true;

				_current.Dispose();
				_current = null;
				NotifyCurrentChanged(null);
				return true;
			}

			if (_current != null) {
				await controller.Restore(_current);
				_current.Dispose();
			}

			_current = controller;

			var cam = _current.GetCamera();
			Camera.SetupCurrent(cam);
			cam.tag = "MainCamera";
			foreach (var c in ComponentExtension.GetComponentsInChildren<Camera>())
				if (c != cam && c.CompareTag("MainCamera"))
					c.tag = "Untagged";

			var eventSystem = _current.GetEventSystem();
			EventSystem.current = eventSystem;
			foreach (var es in ComponentExtension.GetComponentsInChildren<EventSystem>())
				if (es != eventSystem)
					es.gameObject.SetActive(false);

			NotifyCurrentChanged(_current);
			return true;

			void OnRequest(object[] args) {
				if (args.Length > 0 && args[0] is false)
					canChange = false;
			}
		}
	}
}