using api.nox.game.Controllers;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace api.nox.game
{
    public class GameClientSystem : ClientModInitializer
    {
        private ClientModCoreAPI coreAPI;

        private GameObject controllerObject;

        public void OnInitialize(ModCoreAPI api)
        {
        }

        public void OnInitializeClient(ClientModCoreAPI api)
        {
            coreAPI = api;
            api.AssetAPI.LoadLocalWorld("default");
            var controller = api.AssetAPI.GetLocalAsset<GameObject>("prefabs/xr-controller");
            m_controller = Object.Instantiate(controller);
            GameObject.DontDestroyOnLoad(m_controller);
            var XRControllerComponent = m_controller.AddComponent<XRController>();
            XRControllerComponent.enabled = false;
            var DesktopControllerComponent = m_controller.AddComponent<DesktopController>();
            DesktopControllerComponent.enabled = false;
            XRControllerComponent.SetupControllerAtStartup(api);
            m_bindLeftMenu.action.performed += ctx => OnMenuClick(true);
            m_bindRightMenu.action.performed += ctx => OnMenuClick(false);
        }

        private void OnMenuClick(bool isLeft)
        {
            Debug.Log("Menu clicked");
            var menu = OpenMenu();
        }

        public void OnUpdateClient()
        {
        }


        private GameObject m_controller;
        private InputActionReference m_bindLeftMenu => Nox.CCK.Binding.GetBinding("game.lefthand.menu", m_controller);
        private InputActionReference m_bindRightMenu => Nox.CCK.Binding.GetBinding("game.righthand.menu", m_controller);
        private Camera m_headCamera => Nox.CCK.Reference.GetReference("game.camera", m_controller).GetComponent<Camera>();
        private Menu menu;
        private Menu OpenMenu()
        {
            if (menu != null) return menu;
            var menuPrefab = coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/xr-menu");
            var menuObject = Object.Instantiate(menuPrefab);
            var forw = m_headCamera.transform.forward;
            forw.y = m_headCamera.transform.position.y;
            menuObject.transform.position = m_headCamera.transform.position + forw * .5f;
            var rect = menuObject.GetComponentInChildren<Canvas>().GetComponent<RectTransform>();
            menuObject.transform.position = new Vector3(menuObject.transform.position.x, menuObject.transform.position.y - (
                rect.sizeDelta.y * rect.lossyScale.y
            )*2, menuObject.transform.position.z);
            menuObject.transform.LookAt(m_headCamera.transform, Vector3.up);
            menuObject.transform.Rotate(0, 180, 0);
            menu = menuObject.GetComponent<Menu>();
            return menu;
        }

        public void OnDispose()
        {
        }

        
    }
}