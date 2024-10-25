using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using static UnityEngine.InputSystem.InputAction;
using Logger = Nox.CCK.Logger;

namespace api.nox.game
{
    public class UI_Loggerger : MonoBehaviour
    {
        NearFarInteractor nearFarInteractor;
        void Start()
        {
            nearFarInteractor = GetComponent<NearFarInteractor>();
            nearFarInteractor.selectEntered.AddListener(OnSelectEntered);
            nearFarInteractor.selectExited.AddListener(OnSelectExited);
            nearFarInteractor.hoverEntered.AddListener(OnHoverEntered);
            nearFarInteractor.hoverExited.AddListener(OnHoverExited);
            nearFarInteractor.uiHoverEntered.AddListener(OnUIHoverEntered);
            nearFarInteractor.uiHoverExited.AddListener(OnUIHoverExited);

            nearFarInteractor.uiPressInput.inputActionReferencePerformed.action.performed += OnUIPressPerformed;
        }

        void OnUIPressPerformed(CallbackContext context)
        {
            Logger.Log("UI Press Performed", gameObject);
        }

        void OnSelectEntered(SelectEnterEventArgs state)
        {
            Logger.Log("Select Entered", gameObject);
        }

        void OnSelectExited(SelectExitEventArgs state)
        {
            Logger.Log("Select Exited", gameObject);
        }

        void OnHoverEntered(HoverEnterEventArgs state)
        {
            Logger.Log("Hover Entered", gameObject);
        }

        void OnHoverExited(HoverExitEventArgs state)
        {
            Logger.Log("Hover Exited", gameObject);
        }

        void OnUIHoverEntered(UIHoverEventArgs state)
        {
            Logger.Log("UI Hover Entered", gameObject);
        }

        void OnUIHoverExited(UIHoverEventArgs state)
        {
            Logger.Log("UI Hover Exited", gameObject);
        }
    }
}
