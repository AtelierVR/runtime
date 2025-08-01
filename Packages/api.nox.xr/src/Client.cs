using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.UI;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.xr {
	public class Client : ClientModInitializer {
		internal static Client           Instance;
		internal static ClientModCoreAPI CoreAPI;

		internal static IUiAPI UiAPI
			=> CoreAPI.ModAPI.GetMod("ui")
				.GetClients()
				.FirstOrDefault() as IUiAPI;
		
		#if UNITY_EDITOR
		private static bool NoVRFlag {
			get => Config.LoadEditor().Get("no-vr", false);
			set {
				var config = Config.LoadEditor();
				config.Set("no-vr", value);
				config.Save();
			}
		}

		[UnityEditor.MenuItem("Nox/XR/Enable VR")]
		public static void EnableVR()
			=> NoVRFlag = false;

		[UnityEditor.MenuItem("Nox/XR/Disable VR")]
		public static void DisableVR()
			=> NoVRFlag = true;
		#else
        private static bool NoVRFlag
            => System.Array.Exists(
                System.Environment.GetCommandLineArgs(),
                arg => arg == "--no-vr"
            );
		#endif

		private bool _isXRInitialized;

		[NoxPublic(NoxAccess.Read)] public readonly UnityEvent<bool> OnXRHeadsetChange = new();


		[NoxPublic(NoxAccess.Method)]
		public bool IsXRInitialized()
			=> _isXRInitialized;

		[NoxPublic(NoxAccess.Method)]
		public bool IsReady()
			=> IsXRInitialized() && HasHeadset();

		public async UniTask OnInitializeClientAsync(ClientModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;

			if (NoVRFlag) {
				Logger.LogWarning("VR disabled by flag.");
				return;
			}

			await StartLoader();
		}

		public void OnDisposeClient() {
			StopLoader();
			if (XRController.Remove())
				Logger.Log("XR Controller has been removed.");
			Instance = null;
			CoreAPI  = null;
		}

		private void OnDeviceConnected(InputDevice device) {
			Logger.LogDebug($"New XR Device:");
			Logger.LogDebug(" - name: "            + device.name);
			Logger.LogDebug(" - characteristics: " + device.characteristics);
			Logger.LogDebug(" - manufacturer: "    + device.manufacturer);
			Logger.LogDebug(" - serial number: "   + device.serialNumber);
			Logger.LogDebug(" - subsystem: "       + device.subsystem);

			var usages = new List<InputFeatureUsage>();
			device.TryGetFeatureUsages(usages);
			foreach (var usage in usages)
				Logger.LogDebug(" - usage: " + usage.name);

			if (device.TryGetHapticCapabilities(out var hapticCapabilities)) {
				Logger.LogDebug(" - haptic capabilities:");
				Logger.LogDebug("   - num channels: "        + hapticCapabilities.numChannels);
				Logger.LogDebug("   - supports buffer: "     + hapticCapabilities.supportsBuffer);
				Logger.LogDebug("   - supports impulse: "    + hapticCapabilities.supportsImpulse);
				Logger.LogDebug("   - buffer optimal size: " + hapticCapabilities.bufferOptimalSize);
				Logger.LogDebug("   - buffer max size: "     + hapticCapabilities.bufferMaxSize);
				Logger.LogDebug("   - buffer frequency Hz: " + hapticCapabilities.bufferFrequencyHz);
			}

			if (device.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted)) {
				OnXRHeadsetChange.Invoke(true);
				if (XRController.Make())
					Logger.Log("XR Controller has been created.");
				else Logger.LogWarning("Failed to create XR Controller.");
			}
		}

		private void OnDeviceDisconnected(InputDevice device) {
			Logger.Log($"XR Device disconnected: {device.name} {device.characteristics}");
			if (device.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted)) {
				OnXRHeadsetChange.Invoke(false);
				if (XRController.Remove())
					Logger.Log("XR Controller has been removed.");
				else Logger.LogWarning("Failed to remove XR Controller.");
			}
		}


		[NoxPublic(NoxAccess.Method)]
		public async UniTask StartLoader() {
			if (_isXRInitialized) {
				Logger.LogWarning("XR already initialized.");
				return;
			}

			if (!XRGeneralSettings.Instance.Manager.isInitializationComplete) {
				Logger.Log("Initializing XR...");
				await XRGeneralSettings.Instance.Manager.InitializeLoader().ToUniTask();
			}

			var loader = XRGeneralSettings.Instance.Manager.activeLoader;

			if (!loader) {
				Logger.LogWarning("XR loader is not active.");
				return;
			}

			Logger.Log("Loading XR...");

			if (!loader.Initialize()) {
				Logger.LogError("XR loader failed to initialize.");
				return;
			}

			Logger.Log($"XR loader initialized: {loader.name}");


			Logger.Log("XR initialized. Starting subsystems...");
			XRGeneralSettings.Instance.Manager.StartSubsystems();

			_isXRInitialized = true;

			InputDevices.deviceConnected    += OnDeviceConnected;
			InputDevices.deviceDisconnected += OnDeviceDisconnected;

			var devices = new List<InputDevice>();
			InputDevices.GetDevices(devices);
			foreach (var device in devices)
				OnDeviceConnected(device);
		}

		public void StopLoader() {
			if (!_isXRInitialized) {
				Logger.LogWarning("XR not initialized.");
				return;
			}

			Logger.Log("Stopping XR...");
			XRGeneralSettings.Instance.Manager.StopSubsystems();
			XRGeneralSettings.Instance.Manager.DeinitializeLoader();
			_isXRInitialized = false;

			InputDevices.deviceConnected    -= OnDeviceConnected;
			InputDevices.deviceDisconnected -= OnDeviceDisconnected;

			OnXRHeadsetChange.Invoke(false);
			Logger.Log("XR stopped.");
		}


		[NoxPublic(NoxAccess.Method)]
		public bool HasHeadset() {
			var devices = new List<InputDevice>();
			InputDevices.GetDevicesAtXRNode(XRNode.Head, devices);
			return devices.Count > 0;
		}

		[NoxPublic(NoxAccess.Method)]
		public bool HasHandRight() {
			var devices = new List<InputDevice>();
			InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
			return devices.Count > 0;
		}

		[NoxPublic(NoxAccess.Method)]
		public bool HasHandLeft() {
			var devices = new List<InputDevice>();
			InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);
			return devices.Count > 0;
		}

		[NoxPublic(NoxAccess.Method)]
		public bool HasHand()
			=> HasHandRight() && HasHandLeft();
	}
}