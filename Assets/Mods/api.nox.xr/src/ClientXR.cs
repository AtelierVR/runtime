using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using System;
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
            if (NoVRFlag)
            {
                Logger.LogWarning("VR disabled by flag.");
                return;
            }

            await StartLoaderAsync();
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

            if (!XRGeneralSettings.Instance.Manager.activeLoader)
            {
                Logger.LogError("XR loader is not active.");
                return;
            }

            Logger.Log("XR initialized. Starting subsystems...");
            XRGeneralSettings.Instance.Manager.StartSubsystems();

            _isXRActive = XRSettings.isDeviceActive;
            _isXRInitialized = true;
            OnXRHeadsetChange.Invoke(_isXRActive);

            if (!XRSettings.isDeviceActive)
            {
                Logger.LogWarning("XR device is not active.");
                return;
            }

            Logger.Log("XR present. Starting in VR mode.");
        }

        public void OnDisposeClient()
        {
            if (!XRGeneralSettings.Instance.Manager.activeLoader) return;
            Logger.Log("Stopping XR...");
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            _isXRInitialized = false;
        }

        public void OnUpdateClient()
        {
            if (_isXRActive == XRSettings.isDeviceActive) return;
            _isXRActive = XRSettings.isDeviceActive;
            Logger.Log($"XR headset change: {_isXRActive}");
            OnXRHeadsetChange.Invoke(_isXRActive);
        }
    }
}