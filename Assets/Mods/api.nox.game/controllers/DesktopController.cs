using api.nox.game.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Logger = Nox.CCK.Logger;

namespace api.nox.game.Controllers
{
    public class DesktopController : BaseController
    {
        [Header("Desktop Settings")]

        public InputActionReference ToggleMenuAction;
        public InputActionReference ToggleMiniMenuAction;
        public InputActionReference JumpAction;
        public InputActionReference CrouchAction;
        public InputActionReference MicrophoneAction;

        public InputActionReference ForwardAction;
        public InputActionReference BackwardAction;
        public InputActionReference LeftAction;
        public InputActionReference RightAction;

        public EventSystem eventSystem;

        public override uint Priority => 1;

        public override void OnControllerEnable(BaseController last)
        {
            base.OnControllerEnable(last);
            ToggleMenuAction.action.Enable();
            ToggleMiniMenuAction.action.Enable();
            JumpAction.action.Enable();
            CrouchAction.action.Enable();
            MicrophoneAction.action.Enable();
            ForwardAction.action.Enable();
            BackwardAction.action.Enable();
            LeftAction.action.Enable();
            RightAction.action.Enable();
        }

        public override void OnControllerDisable(BaseController next)
        {
            base.OnControllerDisable(next);
            ToggleMenuAction.action.Disable();
            ToggleMiniMenuAction.action.Disable();
            JumpAction.action.Disable();
            CrouchAction.action.Disable();
            MicrophoneAction.action.Disable();
            ForwardAction.action.Disable();
            BackwardAction.action.Disable();
            LeftAction.action.Disable();
            RightAction.action.Disable();
        }

        // forwad, backward, left, right
        public Vector4 Mouvement = new Vector4(0, 0, 0, 0);
        private void SendMouvement()
            => Move(new Vector2(Mouvement.x - Mouvement.y, Mouvement.z - Mouvement.w));

        public override void OnInitialize()
        {
            base.OnInitialize();
            JumpAction.action.performed += _ => Jump();
            CrouchAction.action.performed += _ => IsCrounching = !IsCrounching;
            MicrophoneAction.action.performed += _ => UseMicrophone = !UseMicrophone;
            ToggleMenuAction.action.performed += _ =>
            {
                Logger.Log("Toggle menu");
                // check if a input firld is selected
                if (eventSystem.currentSelectedGameObject != null)
                {
                    Logger.Log("Input field selected");
                    return;
                }
                var menu = MenuManager.Instance.GetViewPortMenu();
                if (menu != null) menu.IsVisible = !menu.IsVisible;
            };

            ForwardAction.action.performed += _ =>
            {
                Mouvement.x = ForwardAction.action.ReadValue<float>();
                SendMouvement();
            };
            ForwardAction.action.canceled += _ =>
            {
                Mouvement.x = 0;
                SendMouvement();
            };

            BackwardAction.action.performed += _ =>
            {
                Mouvement.y = BackwardAction.action.ReadValue<float>();
                SendMouvement();
            };
            BackwardAction.action.canceled += _ =>
            {
                Mouvement.y = 0;
                SendMouvement();
            };

            LeftAction.action.performed += _ =>
            {
                Mouvement.z = LeftAction.action.ReadValue<float>();
                SendMouvement();
            };
            LeftAction.action.canceled += _ =>
            {
                Mouvement.z = 0;
                SendMouvement();
            };

            RightAction.action.performed += _ =>
            {
                Mouvement.w = RightAction.action.ReadValue<float>();
                SendMouvement();
            };
            RightAction.action.canceled += _ =>
            {
                Mouvement.w = 0;
                SendMouvement();
            };

        }

        void Update()
        {
            // use horizontal and vertical axis
            // use mouse to rotate camera

            // var horizontal = Input.GetAxis("Horizontal");
            // var vertical = Input.GetAxis("Vertical");
            // var mouse = Mouse.current.delta.ReadValue();

            // if (horizontal != 0 || vertical != 0)
            //     Move(new Vector2(horizontal, vertical));

            // if (mouse.x != 0 || mouse.y != 0)
            //     Rotation *= Quaternion.Euler(-mouse.y, mouse.x, 0);

        }
    }
}