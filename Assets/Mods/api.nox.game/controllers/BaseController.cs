using System.Collections.Generic;
using Nox.CCK.Mods.Cores;
using UnityEngine;

namespace api.nox.game.Controllers
{
    public abstract class BaseController : MonoBehaviour
    {
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

        public abstract void Teleport();


        internal void SetupControllerAtStartup(ClientModCoreAPI coreAPI)
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
            }
            else
            {
                if (!gameObject.TryGetComponent<DesktopController>(out var co))
                    throw new System.Exception("DesktopController not found");
                foreach (var controller in controllers)
                    controller.enabled = co == controller;
            }
        }
    }
}