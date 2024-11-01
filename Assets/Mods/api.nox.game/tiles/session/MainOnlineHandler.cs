using api.nox.game.sessions;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.game.Tiles.session
{
    public class MainOnlineHandler
    {

        private SessionHandler _handler;
        internal MainOnlineHandler()
        {
            _handler = new SessionHandler()
            {
                id = "main_online",
                text_key = "session.main_online",
                title_key = "session.main_online",
                icon = null,
                CanSelect = GetCanSelect,
                OnSelected = OnSelected,
                OnDeselected = OnDeselected,
                GetContent = GetContent,
                OnUpdate = OnUpdate
            };
        }

        private bool GetCanSelect(TileObject tileObject)
        {
            var session = tileObject.GetData<Session>(0);
            return session != null && session.Controller != null && session.Controller.GetType() == typeof(OnlineController);
        }

        public void OnSelected(TileObject tileObject, GameObject gameObject) { }
        public void OnDeselected(TileObject tileObject, GameObject gameObject) { }
        public void OnUpdate(TileObject tileObject, GameObject gameObject, Transform transform)
        {
            var session = tileObject.GetData<Session>(0);
            var controller = session.Controller as OnlineController;

            Reference.GetReference("description", gameObject).GetComponent<TextLanguage>()
                .UpdateText(new string[] { controller.GetDescription() });
        }

        public GameObject GetContent(TileObject tileObject, GameObject content, Transform transform)
        {
            var session = tileObject.GetData<Session>(0);

            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/session/main_online");
            prefab.SetActive(false);

            var instance = Object.Instantiate(prefab, transform);

            var leaveButton = Reference.GetReference("leave.button", instance).GetComponent<Button>();

            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(() => OnLeaveClick(tileObject, leaveButton, content, transform).Forget());




            instance.SetActive(true);

            return instance;
        }

        private async UniTask OnLeaveClick(TileObject tileObject, Button leaveButton, GameObject content, Transform transform)
        {
            if (!leaveButton.interactable) return;
            leaveButton.interactable = false;
            var session = tileObject.GetData<Session>(0);
            await SessionManager.Instance.SetSession(null);
            await session.Close();
            leaveButton.interactable = true;
        }

        internal void UpdateHandler()
        {
            GameClientSystem.CoreAPI.EventAPI.Emit("game.session", _handler);
        }

        internal void OnDispose()
        {
            _handler.GetContent = null;
            UpdateHandler();
            _handler = null;
        }
    }
}