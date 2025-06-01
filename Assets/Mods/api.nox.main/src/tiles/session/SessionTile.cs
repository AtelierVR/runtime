/*using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.game.Tiles.session;
using api.nox.game.UI;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Events;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Logger = Nox.CCK.Utils.Logger;
using Nox.CCK.Utils;
using Nox.CCK.Language;
using Transform = UnityEngine.Transform;

namespace api.nox.game.Tiles
{
    internal class SessionTileManager : TileManager
    {
        private Dictionary<string, SessionHandler> _sessionHandlers;
        private EventSubscription _sub;
        private HandlerUpdatedEvent _onHandlerUpdated;
        private readonly MainOnlineHandler _mainOnlineHandler;

        private Dictionary<string, SessionHandler> GetSelectableSessionHandlers(TileObject tile)
        {
            var handlers = new Dictionary<string, SessionHandler>();
            foreach (var handler in _sessionHandlers)
                if (handler.Value.CanSelect != null && handler.Value.CanSelect(tile))
                    handlers.Add(handler.Key, handler.Value);
            return handlers;
        }


        [Serializable]
        public class HandlerUpdatedEvent : UnityEvent<SessionHandler>
        {
        }


        internal SessionTileManager()
        {
            _onHandlerUpdated = new HandlerUpdatedEvent();
            _sub = GameClientSystem.CoreAPI.EventAPI.Subscribe("game.session", OnSessionHandler);
            _sessionHandlers = new Dictionary<string, SessionHandler>();
            _mainOnlineHandler = new MainOnlineHandler();
        }


        internal void PostInitialize()
        {
            Logger.Log("NavigationTileManager.PostInitialize");
            _mainOnlineHandler.UpdateHandler();
        }

        public void OnDispose()
        {
            _mainOnlineHandler.OnDispose();
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_sub);
            _sub = null;
            _onHandlerUpdated.RemoveAllListeners();
            _onHandlerUpdated = null;
            _sessionHandlers = null;
        }

        private void OnSessionHandler(EventData context)
        {
            if (context.Data[0] is not SessionHandler handler) return;
            if (_sessionHandlers.ContainsKey(handler.id) && handler.GetContent == null)
            {
                _sessionHandlers.Remove(handler.id);
                // // if (tile != null) UpdateContent(tile);
                // if (selectedHandler == handler.id)
                //     OnSelectHandler(null, null, null);
                return;
            }

            if (handler.GetContent == null) return;
            _sessionHandlers[handler.id] = handler;
        }


        private class SessionTileObject : TileObject
        {
            public INoxObject Session
            {
                get => GetData<INoxObject>(0);
                set => SetData(0, value);
            }

            public INoxObject PreferSession
                => Session
                   ?? (GameClientSystem.SessionAPI.GetField<ushort>("CurrentUid") != ushort.MaxValue
                       ? GameClientSystem.SessionAPI.CallMethod<INoxObject>("GetCurrentSession")
                       : null);

            public ushort[] SessionUIDs = Array.Empty<ushort>();

            public string SelectedHandler
            {
                get => GetData<string>(1);
                set => SetData(1, value);
            }
        }

        /// <summary>
        /// Send a tile to the menu system
        /// </summary>
        /// <param name="context"></param>
        internal void SendTile(EventData context)
        {
            var tile = new SessionTileObject() { id = "api.nox.game.world", context = context };
            tile.Session ??= tile.PreferSession;
            tile.GetContent = OnGetContent;
            tile.onDisplay = (_, gameObject) => OnDisplay(tile, gameObject);
            MenuManager.Instance.SendTile(tile.MenuId, tile);
        }

        /// <summary>
        /// Get the content of the tile
        /// </summary>
        /// <param name="tf"></param>
        /// <returns>Content of the tile</returns>
        private static GameObject OnGetContent(Transform tf)
        {
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/session/content.prefab");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = "game.session";
            content.SetActive(true);
            return content;
        }

        /// <summary>
        /// Handle the display of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        private void OnDisplay(SessionTileObject tile, GameObject content)
        {
            UpdateAllContent(tile, content);
        }

        private void UpdateAllContent(SessionTileObject tile, GameObject content)
        {
            UpdateOptions(tile, content);
            UpdateContent(tile, content);
            UpdateSessionList(tile, content);
        }

        private void UpdateOptions(SessionTileObject tile, GameObject content)
        {
            Logger.Log("Updating options");

            var options = Reference.GetReference("options", content)?.transform;
            if (options == null) return;
            foreach (Transform child in options)
                Object.Destroy(child.gameObject);

            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/ui/standard.list.prefab");
            prefab.SetActive(false);

            foreach (var handler in GetSelectableSessionHandlers(tile))
            {
                var obj = Object.Instantiate(prefab, options);

                var withIcon = Reference.GetReference("with_icon", obj);
                var noIcon = Reference.GetReference("no_icon", obj);

                if (handler.Value.icon != null)
                {
                    withIcon.SetActive(true);
                    noIcon.SetActive(false);

                    if (Reference.TryGetReference("icon", out var icon, withIcon) &&
                        icon.TryGetComponent<RawImage>(out var rawImage))
                        rawImage.texture = handler.Value.icon;
                    if (Reference.TryGetReference("text", out var text, withIcon) &&
                        text.TryGetComponent<TextLanguage>(out var textLanguage))
                        textLanguage.UpdateText(handler.Value.text_key);
                }
                else
                {
                    withIcon.SetActive(false);
                    noIcon.SetActive(true);

                    if (Reference.TryGetReference("text", out var text, noIcon) &&
                        text.TryGetComponent<TextLanguage>(out var textLanguage))
                        textLanguage.UpdateText(handler.Value.text_key);
                }

                obj.SetActive(true);
                obj.GetComponent<Button>().onClick.AddListener(() => OnSelectHandler(handler.Value));
            }
        }

        private static void OnSelectHandler(SessionHandler handler)
        {
            Logger.Log("Selected handler: " + handler.id);
        }

        private void UpdateContent(SessionTileObject tile, GameObject content)
        {
            Logger.Log("Updating content");

            if (tile.Session == null) return;

            var session = tile.Session;
            var controller = session.CallMethod<INoxObject>("GetController");
            var title = controller.CallMethod<string>("GetTitle");
            var urlThumbnail = controller.CallMethod<string>("GetThumbnail");

            Reference.GetReference("title", content)
                .GetComponent<TextLanguage>()
                .UpdateText(new[] { title });

            var thumbnail = Reference.GetReference("thumbnail", content).GetComponent<RawImage>();
            UpdateThumbnail(urlThumbnail, thumbnail).Forget();

            SessionHandler handler = null;
            var selectableSessionHandlers = GetSelectableSessionHandlers(tile);

            if (!string.IsNullOrEmpty(tile.SelectedHandler) &&
                selectableSessionHandlers.TryGetValue(tile.SelectedHandler, out var sessionHandler))
                handler = sessionHandler;
            else if (selectableSessionHandlers.Count > 0)
                handler = selectableSessionHandlers.Values.FirstOrDefault();

            if (handler == null) return;
            var handlerContent = Reference.GetReference("content", content).transform;
            if (handler.id != tile.SelectedHandler)
            {
                tile.SelectedHandler = handler.id;

                foreach (Transform child in handlerContent)
                    Object.Destroy(child.gameObject);

                handler.GetContent(tile, content, handlerContent);
            }

            handler.OnUpdate?.Invoke(tile, content, handlerContent);
        }

        private static async UniTaskVoid UpdateThumbnail(string url, RawImage thumbnail)
        {
            if (!thumbnail || string.IsNullOrEmpty(url)) return;
            var texture = await GameClientSystem.NetworkAPI.CallAsyncMethod<Texture2D>("FetchTexture", url);
            if (!texture) return;
            thumbnail.texture = texture;
        }

        private void UpdateSessionList(SessionTileObject tile, GameObject content)
        {
            var selectSession = Reference.GetReference("select_session", content).GetComponent<TMP_Dropdown>();

            selectSession.ClearOptions();
            var sessions = GameClientSystem.SessionAPI.CallMethod<INoxObject[]>("GetSessions");

            var options = new List<TMP_Dropdown.OptionData>();
            var ids = new List<ushort>();
            foreach (var session in sessions)
            {
                var controller = session.CallMethod<INoxObject>("GetController");
                if (controller == null) continue;
                var option = new TMP_Dropdown.OptionData(controller.CallMethod<string>("GetTitle"));
                ids.Add(session.GetField<ushort>("Uid"));
                options.Add(option);
            }

            selectSession.AddOptions(options);
            tile.SessionUIDs = ids.ToArray();

            selectSession.onValueChanged.RemoveAllListeners();

            var sUid = tile.Session?.GetField<ushort>("Uid") ?? ushort.MaxValue;
            for (var i = 0; i < tile.SessionUIDs.Length; i++)
                if (tile.SessionUIDs[i] == sUid)
                {
                    selectSession.value = i;
                    break;
                }

            selectSession.onValueChanged.AddListener(value =>
            {
                var id = value < tile.SessionUIDs.Length ? tile.SessionUIDs[value] : ushort.MaxValue;
                if (id == ushort.MaxValue) return;
                OnSelectSession(tile, content, id);
            });
        }

        private void OnSelectSession(SessionTileObject tile, GameObject content, ushort uid)
        {
            var session = GameClientSystem.SessionAPI.CallMethod<INoxObject>("GetSession", uid);
            if (session == null) return;
            tile.Session = session;
            UpdateAllContent(tile, content);
        }
    }
}*/