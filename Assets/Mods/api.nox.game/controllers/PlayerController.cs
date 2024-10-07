using System;
using UnityEngine;

namespace api.nox.game.Controllers
{
    public class PlayerController : MonoBehaviour, IDisposable
    {
        internal static PlayerController Instance;
        public static BaseController GetCurrentController() => Instance.CurrentController;

        [Header("Controllers")]
        public BaseController[] controllers;

        [Header("UI")]
        public Canvas ViewPort;
        public RectTransform Container;

        /// <summary>
        /// Resize the container to fit the viewport
        /// </summary>
        void LateUpdate()
        {
            var x = ViewPort.pixelRect.width / Container.rect.width;
            var y = ViewPort.pixelRect.height / Container.rect.height;
            var bestScale = x < y ? x : y;
            Container.localScale = new Vector3(bestScale, bestScale, 1);
        }

        /// <summary>
        /// Estimate the best controller to use
        /// </summary>
        public BaseController EstimateController()
        {
            BaseController bestController = null;
            foreach (BaseController controller in controllers)
                if (bestController == null)
                    bestController = controller;
                else if (controller.Priority > bestController.Priority)
                    bestController = controller;
            return bestController;
        }


        /// <summary>
        /// Get or set the current controller
        /// </summary>
        public BaseController CurrentController
        {
            get
            {
                foreach (BaseController controller in controllers)
                    if (controller.gameObject.activeSelf)
                        return controller;
                return null;
            }
            set
            {
                var current = CurrentController;
                foreach (BaseController controller in controllers)
                    if (controller == value && !controller.gameObject.activeSelf)
                    {
                        controller.gameObject.SetActive(true);
                        controller.OnControllerEnable(current);
                    }
                    else if (controller != value && controller.gameObject.activeSelf)
                    {
                        controller.gameObject.SetActive(false);
                        controller.OnControllerDisable(value);
                    }
                    else controller.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Set estimate the best controller to use
        /// </summary>
        void Awake()
        {
            Instance = this;
            foreach (Transform child in Container)
                Destroy(child.gameObject);
            foreach (BaseController controller in controllers)
                controller.gameObject.SetActive(false);
            CurrentController = EstimateController();
        }

        void OnDestroy() => Instance = Instance == this ? null : Instance;
        public void Dispose()
        {
            Destroy(gameObject);
        }
    }
}