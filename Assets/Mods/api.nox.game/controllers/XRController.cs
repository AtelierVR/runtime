using System.Runtime.InteropServices;
using Autohand;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace api.nox.game.Controllers
{

    public class XRController : BaseController
    {
        [Header("XR Settings")]

        public InputActionReference ToggleMenuAction;
        public InputActionReference ToggleMiniMenuAction;
        public InputActionReference JumpAction;
        public InputActionReference CrouchAction;
        public InputActionReference MicrophoneAction;
        public InputActionManager InputAction;

        public override uint Priority => (uint)(GameSystem.instance.coreAPI.XRAPI.IsEnabled() ? 2 : 0);


        public override void OnControllerEnable(BaseController last)
        {
            base.OnControllerEnable(last);
            ToggleMenuAction.action.Enable();
            ToggleMiniMenuAction.action.Enable();
            JumpAction.action.Enable();
            CrouchAction.action.Enable();
            MicrophoneAction.action.Enable();
            InputAction.enabled = false;
            UniTask.RunOnThreadPool(async () =>
            {
                await UniTask.WaitForSeconds(.1f);
                InputAction.enabled = true;
            }).Forget();
        }

        public override void OnControllerDisable(BaseController next)
        {
            base.OnControllerDisable(next);
            ToggleMenuAction.action.Disable();
            ToggleMiniMenuAction.action.Disable();
            JumpAction.action.Disable();
            CrouchAction.action.Disable();
            MicrophoneAction.action.Disable();
            InputAction.enabled = false;
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
            JumpAction.action.performed += _ => Jump();
            CrouchAction.action.performed += _ => IsCrounching = !IsCrounching;
            MicrophoneAction.action.performed += _ => UseMicrophone = !UseMicrophone;
        }

        private void SetRotationType(XRRotationType type) => Player.rotationType = type switch
        {
            XRRotationType.Smooth => Autohand.RotationType.smooth,
            XRRotationType.Snap => Autohand.RotationType.snap,
            _ => Player.rotationType
        };

        private XRRotationType GetRotationType() => Player.rotationType switch
        {
            Autohand.RotationType.smooth => XRRotationType.Smooth,
            Autohand.RotationType.snap => XRRotationType.Snap,
            _ => XRRotationType.Smooth
        };

        public XRRotationType RotationType
        {
            get => GetRotationType();
            set => SetRotationType(value);
        }

        public float SmoothTurnSpeed
        {
            get => Player.smoothTurnSpeed;
            set => Player.smoothTurnSpeed = value;
        }
        public float SnapTurnAngle
        {
            get => Player.snapTurnAngle;
            set => Player.snapTurnAngle = value;
        }
    }

    public enum XRRotationType
    {
        Smooth,
        Snap
    }
}