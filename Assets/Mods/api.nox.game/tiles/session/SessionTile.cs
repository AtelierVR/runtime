using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.game.sessions;
using api.nox.game.Tiles.session;
using api.nox.game.UI;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods.Events;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Logger = Nox.CCK.Logger;
using Object = UnityEngine.Object;

namespace api.nox.game.Tiles
{
    internal class SessionTileManager : TileManager
    {
        internal static SessionTileManager Instance;
        internal Dictionary<string, SessionHandler> sessionHandlers = null;

        internal Dictionary<string, SessionHandler> GetSelectableSessionHandlers(TileObject tile)
        {
            var handlers = new Dictionary<string, SessionHandler>();
            foreach (var handler in sessionHandlers)
                if (handler.Value.CanSelect != null && handler.Value.CanSelect(tile))
                    handlers.Add(handler.Key, handler.Value);
            return handlers;
        }

        private EventSubscription _sub;

        [Serializable] public class HandlerUpdatedEvent : UnityEvent<SessionHandler> { }
        public HandlerUpdatedEvent OnHandlerUpdated;

        private MainOnlineHandler MainOnlineHandler;

        internal SessionTileManager()
        {
            Instance = this;
            OnHandlerUpdated = new HandlerUpdatedEvent();
            _sub = GameClientSystem.CoreAPI.EventAPI.Subscribe("game.session", OnSessionHandler);

            sessionHandlers = new Dictionary<string, SessionHandler>();
            MainOnlineHandler = new MainOnlineHandler();
        }


        internal void PostInitialize()
        {
            Logger.Log("NavigationTileManager.PostInitialize");
            MainOnlineHandler.UpdateHandler();
        }

        public void OnDispose()
        {
            MainOnlineHandler.OnDispose();
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_sub);
            _sub = null;
            OnHandlerUpdated.RemoveAllListeners();
            OnHandlerUpdated = null;
            sessionHandlers.Clear();
            sessionHandlers = null;
            Instance = null;
        }

        private void OnSessionHandler(EventData context)
        {
            if (context.Data[0] is not SessionHandler handler) return;
            if (sessionHandlers.ContainsKey(handler.id) && handler.GetContent == null)
            {
                sessionHandlers.Remove(handler.id);
                // // if (tile != null) UpdateContent(tile);
                // if (selectedHandler == handler.id)
                //     OnSelectHandler(null, null, null);
                return;
            }
            if (handler.GetContent == null) return;
            if (sessionHandlers.ContainsKey(handler.id))
                sessionHandlers[handler.id] = handler;
            else sessionHandlers.Add(handler.id, handler);
        }


        internal class SessionTileObject : TileObject
        {
            public Session Session
            {
                get => GetData<Session>(0);
                set => SetData(0, value);
            }

            public Session PreferedSession => Session
                ?? (SessionManager.Instance.currentSessionUid != ushort.MaxValue
                ? SessionManager.Instance.GetSession(SessionManager.Instance.currentSessionUid)
                : null);

            public ushort[] sessionUids = new ushort[0];

            public string selectedHandler
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
            if (tile.Session == null) tile.Session = tile.PreferedSession;
            tile.GetContent = (Transform tf) => OnGetContent(tile, tf);
            tile.onDisplay = (str, gameObject) => OnDisplay(tile, gameObject);
            MenuManager.Instance.SendTile(tile.MenuId, tile);
        }

        /// <summary>
        /// Get the content of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="tf"></param>
        /// <returns>Content of the tile</returns>
        internal GameObject OnGetContent(SessionTileObject tile, Transform tf)
        {
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/session/content");
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
        internal void OnDisplay(SessionTileObject tile, GameObject content)
        {
            UpdateAllContent(tile, content);
        }

        internal void UpdateAllContent(SessionTileObject tile, GameObject content)
        {
            UpdateOptions(tile, content);
            UpdateContent(tile, content);
            UpdateSessionList(tile, content);
        }

        internal void UpdateOptions(SessionTileObject tile, GameObject content)
        {
            Logger.Log("Updating options");

            var options = Reference.GetReference("options", content)?.transform;
            foreach (Transform child in options)
                Object.Destroy(child.gameObject);

            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/ui/standard.list");
            prefab.SetActive(false);

            foreach (var handler in GetSelectableSessionHandlers(tile))
            {
                var obj = Object.Instantiate(prefab, options);

                var with_icon = Reference.GetReference("with_icon", obj);
                var no_icon = Reference.GetReference("no_icon", obj);

                if (handler.Value.icon != null)
                {
                    with_icon.SetActive(true);
                    no_icon.SetActive(false);
                    if (Reference.GetReference("icon", with_icon)?.TryGetComponent<RawImage>(out var rawImage) == true)
                        rawImage.texture = handler.Value.icon;
                    if (Reference.GetReference("text", with_icon)?.TryGetComponent<TextLanguage>(out var textLanguage) == true)
                        textLanguage.UpdateText(handler.Value.text_key);
                }
                else
                {
                    with_icon.SetActive(false);
                    no_icon.SetActive(true);
                    if (Reference.GetReference("text", no_icon)?.TryGetComponent<TextLanguage>(out var textLanguage) == true)
                        textLanguage.UpdateText(handler.Value.text_key);
                }

                obj.SetActive(true);
                obj.GetComponent<Button>().onClick.AddListener(() => OnSelectHandler(tile, content, handler.Value));
            }
        }

        internal void OnSelectHandler(SessionTileObject tile, GameObject content, SessionHandler handler)
        {
            /// ...
            Logger.Log("Selected handler: " + handler.id);
        }

        internal void UpdateContent(SessionTileObject tile, GameObject content)
        {
            Logger.Log("Updating content");

            if (tile.Session == null) return;

            Reference.GetReference("title", content).GetComponent<TextLanguage>()
                .UpdateText(new string[] { tile.Session.Controller.GetTitle() });

            var thumbnail = Reference.GetReference("thumbnail", content).GetComponent<RawImage>();
            var urlthumbnail = tile.Session.Controller.GetThumbnail();
            UpdateThumbnail(urlthumbnail, thumbnail).Forget();

            SessionHandler handler = null;
            var selectableSessionHandlers = GetSelectableSessionHandlers(tile);

            if (!string.IsNullOrEmpty(tile.selectedHandler) && selectableSessionHandlers.ContainsKey(tile.selectedHandler))
                handler = selectableSessionHandlers[tile.selectedHandler];
            else if (selectableSessionHandlers.Count > 0)
                handler = selectableSessionHandlers.Values.FirstOrDefault();

            if (handler != null)
            {
                var handlerContent = Reference.GetReference("content", content).transform;
                if (handler.id != tile.selectedHandler)
                {
                    tile.selectedHandler = handler.id;

                    foreach (Transform child in handlerContent)
                        Object.Destroy(child.gameObject);

                    var c = handler.GetContent(tile, content, handlerContent);
                }

                handler.OnUpdate?.Invoke(tile, content, handlerContent);
            }
        }

        internal async UniTaskVoid UpdateThumbnail(string url, RawImage thumbnail)
        {
            if (thumbnail == null || string.IsNullOrEmpty(url)) return;
            var texture = await GameClientSystem.Instance.NetworkAPI.FetchTexture(url);
            if (texture == null) return;
            thumbnail.texture = texture;
        }

        internal void UpdateSessionList(SessionTileObject tile, GameObject content)
        {
            Logger.Log("Updating session list");
            var select_session = Reference.GetReference("select_session", content).GetComponent<TMP_Dropdown>();

            select_session.ClearOptions();
            var sessions = SessionManager.Instance.sessions;

            var options = new List<TMP_Dropdown.OptionData>();
            var ids = new List<ushort>();
            foreach (var session in sessions)
            {
                var option = new TMP_Dropdown.OptionData(session.Controller.GetTitle());
                ids.Add(session.uid);
                options.Add(option);
            }
            select_session.AddOptions(options);
            tile.sessionUids = ids.ToArray();

            select_session.onValueChanged.RemoveAllListeners();

            for (var i = 0; i < tile.sessionUids.Length; i++)
                if (tile.sessionUids[i] == tile.Session.uid)
                {
                    select_session.value = i;
                    break;
                }

            select_session.onValueChanged.AddListener((int value) =>
            {
                var id = value < tile.sessionUids.Length ? tile.sessionUids[value] : ushort.MaxValue;
                if (id == ushort.MaxValue) return;
                OnSelectSession(tile, content, id);
            });
        }

        internal void OnSelectSession(SessionTileObject tile, GameObject content, ushort uid)
        {
            var session = SessionManager.Instance.GetSession(uid);
            if (session == null) return;
            tile.Session = session;

            UpdateAllContent(tile, content);
        }
    }
}