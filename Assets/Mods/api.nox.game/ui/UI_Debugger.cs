using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using static UnityEngine.InputSystem.InputAction;

namespace api.nox.game
{
    public class UI_Debugger : MonoBehaviour
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
            Debug.Log("UI Press Performed", gameObject);
        }

        void OnSelectEntered(SelectEnterEventArgs state)
        {
            Debug.Log("Select Entered", gameObject);
        }

        void OnSelectExited(SelectExitEventArgs state)
        {
            Debug.Log("Select Exited", gameObject);
        }

        void OnHoverEntered(HoverEnterEventArgs state)
        {
            Debug.Log("Hover Entered", gameObject);
        }

        void OnHoverExited(HoverExitEventArgs state)
        {
            Debug.Log("Hover Exited", gameObject);
        }

        void OnUIHoverEntered(UIHoverEventArgs state)
        {
            Debug.Log("UI Hover Entered", gameObject);
        }

        void OnUIHoverExited(UIHoverEventArgs state)
        {
            Debug.Log("UI Hover Exited", gameObject);
        }
    }
}
