/*using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.UI;
using Transform = UnityEngine.Transform;

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
            var session = tileObject.GetData<INoxObject>(0);
            var controller = session?.CallMethod<INoxObject>("GetController");
            if (controller == null) return false;
            return controller.CallMethod<string>("GetTypeName") == "online";
        }

        private void OnSelected(TileObject tileObject, GameObject gameObject)
        {
        }

        private void OnDeselected(TileObject tileObject, GameObject gameObject)
        {
        }

        private void OnUpdate(TileObject tileObject, GameObject gameObject, Transform transform)
        {
            var session = tileObject.GetData<INoxObject>(0);
            var controller = session?.CallMethod<INoxObject>("GetController");
            var desc = controller?.CallMethod<string>("GetDescription");

            Reference.GetReference("description", gameObject)
                .GetComponent<TextLanguage>()
                .UpdateText(new[] { desc ?? "session.main_online.description" });
        }

        private GameObject GetContent(TileObject tileObject, GameObject content, Transform transform)
        {
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/session/main_online.prefab");
            prefab.SetActive(false);

            var instance = Object.Instantiate(prefab, transform);

            var leaveButton = Reference.GetReference("leave.button", instance).GetComponent<Button>();

            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(() => OnLeaveClick(tileObject, leaveButton, content, transform).Forget());

            instance.SetActive(true);

            return instance;
        }

        private async UniTask OnLeaveClick(TileObject tileObject, Button leaveButton, GameObject content,
            Transform transform)
        {
            if (!leaveButton.interactable) return;
            leaveButton.interactable = false;
            var session = tileObject.GetData<INoxObject>(0);
            await GameClientSystem.SessionAPI!.CallMethod<UniTask>("SetCurrentSession", null);
            await session!.CallMethod<UniTask>("Dispose");
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
}*/