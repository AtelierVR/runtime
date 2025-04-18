using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.xr
{
    public class ClientXR : ClientModInitializer
    {
#if UNITY_EDITOR
        private static bool NoVRFlag
        {
            get => Config.LoadEditor().Get("no-vr", false);
            set
            {
                var config = Config.LoadEditor();
                config.Set("no-vr", value);
                config.Save();
            }
        }

        [UnityEditor.MenuItem("Nox/XR/Enable VR")]
        public static void EnableVR() => NoVRFlag = false;

        [UnityEditor.MenuItem("Nox/XR/Disable VR")]
        public static void DisableVR() => NoVRFlag = true;
#else
        private static bool NoVRFlag
            => System.Array.Exists(
                System.Environment.GetCommandLineArgs(),
                arg => arg == "--no-vr"
            );
#endif

        private static bool _isXRActive;
        private static bool _isXRInitialized;

        [NoxPublic(NoxAccess.Read)] public static readonly UnityEvent<bool> OnXRHeadsetChange = new();

        [NoxPublic(NoxAccess.Method)]
        public static bool IsXRActive() => _isXRActive;

        [NoxPublic(NoxAccess.Method)]
        public static bool IsXRInitialized() => _isXRInitialized;


        public async UniTask OnInitializeClientAsync(ClientModCoreAPI api)
        {
            InputDevices.deviceConnected += OnDeviceConnected;
            InputDevices.deviceDisconnected += OnDeviceDisconnected;

            var devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);
            foreach (var device in devices)
                OnDeviceConnected(device);

            if (NoVRFlag)
            {
                Logger.LogWarning("VR disabled by flag.");
                return;
            }

            await StartLoaderAsync();
        }

        public void OnDisposeClient()
        {
            if (!XRGeneralSettings.Instance.Manager.activeLoader) return;
            Logger.Log("Stopping XR...");
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            _isXRInitialized = false;
            InputDevices.deviceConnected -= OnDeviceConnected;
            InputDevices.deviceDisconnected -= OnDeviceDisconnected;
        }

        private void OnDeviceConnected(InputDevice device)
        {
            Logger.LogDebug($"New XR Device:");
            Logger.LogDebug(" - name: " + device.name);
            Logger.LogDebug(" - characteristics: " + device.characteristics);
            Logger.LogDebug(" - manufacturer: " + device.manufacturer);
            Logger.LogDebug(" - serial number: " + device.serialNumber);
            Logger.LogDebug(" - subsystem: " + device.subsystem);

            var usages = new List<InputFeatureUsage>();
            device.TryGetFeatureUsages(usages);
            foreach (var usage in usages)
                Logger.LogDebug(" - usage: " + usage.name);

            if (device.TryGetHapticCapabilities(out var hapticCapabilities))
            {
                Logger.LogDebug(" - haptic capabilities:");
                Logger.LogDebug("   - num channels: " + hapticCapabilities.numChannels);
                Logger.LogDebug("   - supports buffer: " + hapticCapabilities.supportsBuffer);
                Logger.LogDebug("   - supports impulse: " + hapticCapabilities.supportsImpulse);
                Logger.LogDebug("   - buffer optimal size: " + hapticCapabilities.bufferOptimalSize);
                Logger.LogDebug("   - buffer max size: " + hapticCapabilities.bufferMaxSize);
                Logger.LogDebug("   - buffer frequency Hz: " + hapticCapabilities.bufferFrequencyHz);
            }

            if (device.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted))
                OnXRHeadsetChange.Invoke(true);
        }

        private void OnDeviceDisconnected(InputDevice device)
        {
            Logger.Log($"XR Device disconnected: {device.name} {device.characteristics}");
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted))
                OnXRHeadsetChange.Invoke(false);
        }


        [NoxPublic(NoxAccess.Method)]
        public async UniTask StartLoaderAsync()
        {
            if (_isXRInitialized)
            {
                Logger.LogWarning("XR already initialized.");
                return;
            }

            if (!XRGeneralSettings.Instance.Manager.isInitializationComplete)
            {
                Logger.Log("Initializing XR...");
                await XRGeneralSettings.Instance.Manager.InitializeLoader().ToUniTask();
            }

            var loader = XRGeneralSettings.Instance.Manager.activeLoader;

            if (!loader)
            {
                Logger.LogError("XR loader is not active.");
                return;
            }

            Logger.Log("Loading XR...");


            if (!loader.Initialize())
            {
                Logger.LogError("XR loader failed to initialize.");
                return;
            }

            Logger.Log($"XR loader initialized: {loader.name}");


            Logger.Log("XR initialized. Starting subsystems...");
            XRGeneralSettings.Instance.Manager.StartSubsystems();

            _isXRActive = XRSettings.isDeviceActive;
            _isXRInitialized = true;
            OnXRHeadsetChange.Invoke(_isXRActive);
        }


        public void OnUpdateClient()
        {
            if (_isXRActive == XRSettings.isDeviceActive) return;
            _isXRActive = XRSettings.isDeviceActive;
            Logger.Log($"XR headset change: {_isXRActive}");
            OnXRHeadsetChange.Invoke(_isXRActive);
        }

        [NoxPublic(NoxAccess.Method)]
        public bool HasHeadset()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.Head, devices);
            return devices.Count > 0;
        }
    }
}