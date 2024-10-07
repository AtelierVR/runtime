using api.nox.game.UI;
using UnityEngine;
using UnityEngine.InputSystem;

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

        public override uint Priority => 1;

        public override void OnControllerEnable(BaseController last)
        {
            base.OnControllerEnable(last);
            ToggleMenuAction.action.Enable();
            ToggleMiniMenuAction.action.Enable();
            JumpAction.action.Enable();
            CrouchAction.action.Enable();
            MicrophoneAction.action.Enable();
        }

        public override void OnControllerDisable(BaseController next)
        {
            base.OnControllerDisable(next);
            ToggleMenuAction.action.Disable();
            ToggleMiniMenuAction.action.Disable();
            JumpAction.action.Disable();
            CrouchAction.action.Disable();
            MicrophoneAction.action.Disable();
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
            JumpAction.action.performed += _ => Jump();
            CrouchAction.action.performed += _ => IsCrounching = !IsCrounching;
            MicrophoneAction.action.performed += _ => UseMicrophone = !UseMicrophone;
            ToggleMenuAction.action.performed += _ =>
            {
                var menu = MenuManager.Instance.GetViewPortMenu();
                if (menu != null) menu.IsVisible = !menu.IsVisible;
            };
        }
    }
}