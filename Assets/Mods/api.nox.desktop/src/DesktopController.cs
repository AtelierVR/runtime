using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;
using Nox.Controllers;

namespace api.nox.desktop
{
    public class DesktopController : MonoBehaviour, IController, INoxObject
    {
        private static int DefaultPriority
            => Config.Load().Get("settings.controller.desktop_priority", IController.DefaultPriority);

        private const string DefaultId = "desktop";

        /// <summary>
        /// Get the proxy mod API.
        /// </summary>
        private static IControllerAPI ControllerAPI
            => Client.CoreAPI.ModAPI.GetMod("controller").GetMains().FirstOrDefault() as IControllerAPI;

        /// <summary>
        /// Check if the current proxy is better than Desktop proxy.
        /// </summary>
        /// <returns></returns>
        private static bool IsBetterThanCurrent()
        {
            var controller = ControllerAPI.GetCurrent();
            return controller == null
                || controller.GetPriority() < DefaultPriority
                || controller.GetId() == DefaultId;
        }

        /// <summary>
        /// Check if the current proxy is the Desktop proxy.
        /// </summary>
        /// <returns></returns>
        private static bool IsCurrent()
        {
            var controller = ControllerAPI.GetCurrent();
            return controller != null
                && controller.GetId() == DefaultId;
        }

        /// <summary>
        /// Remove the current proxy if it is the Desktop proxy.
        /// </summary>
        internal static bool Remove()
        {
            if (!IsCurrent()) return false;
            ControllerAPI.SetCurrent(null);
            return true;
        }

        /// <summary>
        /// Create the Desktop proxy if it is not already created.
        /// </summary>
        /// <returns></returns>
        internal static bool Make()
        {
            if (!IsBetterThanCurrent()) return false;

            var prefab = Client.CoreAPI.AssetAPI.GetAsset<GameObject>("desktop_proxy.prefab");
            if (!prefab)
            {
                Logger.LogError("Failed to load desktop proxy prefab");
                return false;
            }

            var instance = Instantiate(prefab);
            var desktop = instance.GetComponent<DesktopController>();

            if (!desktop)
            {
                Logger.LogError("Failed to get desktop proxy component");
                Destroy(instance);
                return false;
            }

            if (!ControllerAPI.SetCurrent(desktop))
            {
                Logger.LogError("Failed to set Desktop proxy as current");
                Destroy(instance);
                return false;
            }

            desktop.gameObject.name = $"[{desktop.GetType().Name}_{desktop.GetInstanceID()}]";
            DontDestroyOnLoad(desktop);
            return true;
        }

        [NoxPublic(NoxAccess.Method)]
        public string GetId()
            => DefaultId;

        [NoxPublic(NoxAccess.Method)]
        public int GetPriority()
            => DefaultPriority;

        public DesktopPlayer player;

        public void Dispose()
        {
            Destroy(gameObject);
        }

        [NoxPublic(NoxAccess.Method)]
        public Camera GetCamera()
            => player.headCamera;

        [NoxPublic(NoxAccess.Method)]
        public Collider GetCollider()
            => player.bodyCollider;

        public void Restore(IController controller)
        {
            foreach (var ability in controller.GetAbilities())
                SetAbilities(ability.Key, ability.Value);
        }        [NoxPublic(NoxAccess.Method)]
        public Dictionary<string, object> GetAbilities()
            => new() {
                { "grounded", player.IsGrounded() },
                { "immobilized", !player.useMovement },
                { "crouching", player.crouching },
                { "sprinting", player.IsSprinting() },
                { "flying", player.IsFlying() },
                { "may_fly", player.MayFly() },
                { "max_move_speed", player.maxMoveSpeed },
                { "move_acceleration", player.moveAcceleration },
                { "jump_force", player.jumpForce },
                { "fly_speed", player.flySpeed },
                { "sprint_multiplier", player.sprintMultiplier },
                { "air_control", player.airControl }
            };        [NoxPublic(NoxAccess.Method)]
        public void SetAbilities(string key, object value)
        {
            if (!GetAbilities().ContainsKey(key)) return;
            switch (key)
            {
                case "immobilized":
                    player.useMovement = !(bool)value;
                    break;
                case "crouching":
                    player.SetCrouching((bool)value);
                    break;
                case "sprinting":
                    player.SetSprinting((bool)value);
                    break;
                case "flying":
                    if ((bool)value != player.IsFlying())
                    {
                        player.ToggleFlying();
                    }
                    break;
                case "may_fly":
                    player.SetMayFly((bool)value);
                    break;
                case "max_move_speed":
                    player.maxMoveSpeed = (float)value;
                    break;
                case "move_acceleration":
                    player.moveAcceleration = (float)value;
                    break;
                case "jump_force":
                    player.jumpForce = (float)value;
                    break;
                case "fly_speed":
                    player.flySpeed = (float)value;
                    break;
                case "sprint_multiplier":
                    player.sprintMultiplier = (float)value;
                    break;
                case "air_control":
                    player.airControl = (float)value;
                    break;
            }
        }

        [NoxPublic(NoxAccess.Method)]
        public Dictionary<ushort, Transform> GetParts()
            => new() {
                { PlayerRig.Base.ToIndex(), transform },
                { PlayerRig.Head.ToIndex(), player.headCamera.transform }
            };
    }
}