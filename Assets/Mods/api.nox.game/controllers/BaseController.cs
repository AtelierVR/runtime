using System.Collections.Generic;
using Autohand;
using Nox.CCK.Mods.Cores;
using Nox.SimplyLibs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace api.nox.game.Controllers
{
    public abstract class BaseController : MonoBehaviour
    {
        public AutoHandPlayer Player;
        public static BaseController CurrentController;
        protected ClientModCoreAPI CoreAPI;

        public void SetCoreAPI(ClientModCoreAPI coreAPI)
        {
            CoreAPI = coreAPI;
        }

        // run when the controller is enabled
        public void OnEnable()
        {
            CurrentController = this;
        }

        // run when the controller is disabled
        public void OnDisable()
        {
            if (CurrentController == this)
                CurrentController = null;
        }

        internal static void SetupControllerAtStartup(ClientModCoreAPI coreAPI, GameObject gameObject)
        {
            var controllers = gameObject.GetComponents<BaseController>();
            if (controllers.Length == 0)
                throw new System.Exception("No controllers found");

            foreach (var controller in controllers)
                controller.SetCoreAPI(coreAPI);

            var xrmod = coreAPI.XRAPI;
            if (xrmod != null && xrmod.IsEnabled())
            {
                if (!gameObject.TryGetComponent<XRController>(out var co))
                    throw new System.Exception("XRController not found");
                foreach (var controller in controllers)
                    controller.enabled = co == controller;
                CurrentController = co;
            }
            else
            {
                if (!gameObject.TryGetComponent<DesktopController>(out var co))
                    throw new System.Exception("DesktopController not found");
                foreach (var controller in controllers)
                    controller.enabled = co == controller;
                CurrentController = co;
            }
        }

        // set if the player can move
        public bool CanMovement
        {
            get => Player.useMovement;
            set => Player.useMovement = value;
        }

        // set the speed of the player
        public float MaxSpeed
        {
            get => Player.maxMoveSpeed;
            set => Player.maxMoveSpeed = value;
        }

        // set if the player is crounching
        public bool IsCrounching
        {
            get => Player.crouching;
            set => Player.crouching = value;
        }

        // set if the player can jump
        private bool _canJump;
        public bool CanJump
        {
            get => _canJump;
            set => _canJump = value;
        }

        // make the player jump
        public virtual void Jump() { if (_canJump) Player.Jump(); }


        // make the player teleport to a target
        public virtual void Teleport(Transform target)
        {
            Rotation = target.rotation;
            Position = target.position;
        }

        // set the player rotation
        public Quaternion Rotation
        {
            get => Player.transform.rotation;
            set => Player.SetRotation(value);
        }

        // set the player position
        public Vector3 Position
        {
            get => Player.transform.position;
            set => Player.SetPosition(value);
        }

        public bool IsGrounded() => Player.IsGrounded();
        public InputActionReference OpenMenuAction;
    }
}