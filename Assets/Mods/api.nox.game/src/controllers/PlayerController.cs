using System.Linq;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.game.controllers
{
    public class PlayerController : MonoBehaviour, INoxObject
    {
        public static PlayerController Instance;
        
        [NoxPublic(NoxAccess.Read)] public BaseController currentController;
        [NoxPublic(NoxAccess.Field)] public bool canChange = true;

        private BaseController GetFallBackController()
            => GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/controllers/DesktopController.prefab")
                .GetComponent<BaseController>();

        private BaseController SpawnController(BaseController controller)
        {
            var crtController = currentController;
            var newController = Instantiate(controller, gameObject.transform);
            newController.transform.localPosition = Vector3.zero;
            newController.transform.localRotation = Quaternion.identity;
            newController.transform.localScale = Vector3.one;

            if (crtController)
            {
                crtController.gameObject.SetActive(false);
                newController.OnControllerDisable(crtController);
                crtController.OnControllerDisable(crtController);
            }

            newController.gameObject.SetActive(true);
            newController.OnControllerEnable(newController);

            if (crtController)
            {
                crtController.OnControllerEnable(newController);
                Destroy(crtController.gameObject);
            }

            currentController = newController;
            return newController;
        }


        /// <summary>
        /// Set estimate the best controller to use
        /// </summary>
        private void Awake() => Initialize();

        internal void Initialize(bool force = false)
        {
            if (force && (!enabled || Instance == this)) return;
            Logger.LogDebug("PlayerController.Awake");
            Instance = this;
            SpawnController(GetFallBackController());
        }

        void OnDestroy()
            => Dispose();

        public void Dispose()
        {
            Instance = Instance == this ? null : Instance;
            Destroy(gameObject);
        }

        public static void Create()
        {
            var o = Instantiate(
                GameClientSystem
                    .CoreAPI.AssetAPI
                    .GetAsset<GameObject>("prefabs/controllers/PlayerController.prefab")
            );
            o.name = $"[{nameof(PlayerController)}]";
            DontDestroyOnLoad(o.gameObject);
            o.GetComponent<PlayerController>().Initialize(true);
        }
    }
}