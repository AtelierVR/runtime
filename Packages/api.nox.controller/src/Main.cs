using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Controllers;

namespace api.nox.controller {
	public class Main : IControllerAPI, MainModInitializer {
		public static Main Instance { get; private set; }

		private IController    _current;
		private MainModCoreAPI _coreAPI;

		public void OnInitializeMain(MainModCoreAPI api) {
			Instance = this;
			_coreAPI = api;
			_current = null;
		}

		public async UniTask OnDisposeMainAsync() {
			await SetCurrent(null);
			_coreAPI = null;
			Instance = null;
		}

		public IController GetCurrent()
			=> _current;

		public async UniTask<bool> SetCurrent(IController controller) {
			if (controller == null) {
				if (_current == null)
					return true;

				_current.Dispose();
				_current = null;
				_coreAPI.EventAPI.Emit("controller_changed", null);
				return true;
			}

			if (_current == controller)
				return true;

			var canChange = true;
			_coreAPI.EventAPI.Emit("controller_request_change", controller, new Action<object[]>(OnRequest));
			if (!canChange) {
				Logger.LogWarning("Controller change request was denied");
				return false;
			}

			if (_current != null) {
				await controller.Restore(_current);
				_current.Dispose();
			}

			_current = controller;
			_coreAPI.EventAPI.Emit("controller_changed", _current);
			return true;

			void OnRequest(object[] args) {
				if (args.Length > 0 && args[0] is false)
					canChange = false;
			}
		}
	}
}