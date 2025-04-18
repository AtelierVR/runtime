using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;
using Transform = UnityEngine.Transform;

namespace api.nox.search.client
{
    public class SearchPage
    {
        private static string GetKey() => "search";
        private static EventSubscription _listener;

        public static void Listen()
        {
            Logger.LogDebug("SearchPage.Listen");
            _listener = SearchSystem.CoreAPI.EventAPI.Subscribe("goto_page", OnGotoEvent);
        }

        public static void StopListen()
        {
            SearchSystem.CoreAPI.EventAPI.Unsubscribe(_listener);
        }

        private static void OnGotoEvent(EventData context)
        {
            if (!context.TryGet(0, out int menuId)) return;
            if (!context.TryGet(1, out string pageKey)) return;
            if (pageKey != GetKey()) return;
            var handler = !context.TryGet(2, out string h)
                ? Config.Load().Get<string>("search.last_handler")
                : h;
            var query = context.TryGet(3, out string q) ? q : null;
            var auto = context.TryGet(4, out bool a) && a;
            var page = new SearchPage
            {
                _menuId = menuId,
                HandlerId = handler,
                Query = query ?? string.Empty,
                LastQuery = (query ?? string.Empty) + " ",
            };
            page.Display();
            if (auto) page.Submit().Forget();
        }

        private void Display()
            => SearchSystem.CoreAPI.EventAPI.Emit("display_page", _menuId, new Dictionary<string, object>
            {
                {
                    "key", GetKey()
                }, // id of the page
                {
                    "content", new Func<Transform, GameObject>(OnContent)
                }, // called when the menu need the content of the page (first call)
                /*
                 {
                    "open", new Action<string, GameObject>(OnOpen)
                }, // called once when the page is display for the first time
                 {
                    "restore", (string key, GameObject go) => OnRestore(key, go)
                }, // called when the menu go back from history and display the page again
                {
                    "remove", (GameObject go) => OnRemove(go)
                }, // called when the menu remove the page from history (last call)
                {
                    "display", (string key, GameObject go) => OnDisplay(key, go)
                }, // called when the page is displayed
                {
                    "hide", (string key, GameObject go) => OnHide(key, go)
                } // called when another page is displayed
                */
            });

        private int _menuId;
        private SearchComponent _comportment;
        private string _handlerId;

        internal string HandlerId
        {
            get => _handlerId;
            set
            {
                _handlerId = value;
                var config = Config.Load();
                config.Set("search.last_handler", value);
                config.Save();
            }
        }

        internal string Query = string.Empty;
        internal string LastQuery = string.Empty + " ";

        internal bool IsEmptyQuery => string.IsNullOrEmpty(Query);
        internal bool IsNewQuery => Query != LastQuery;

        internal Handler Handler
        {
            get
            {
                var handler = HandlerId != null
                    ? SearchSystem.Instance.GetHandler(HandlerId)
                    : null;
                handler ??= SearchSystem.Instance.Handlers.FirstOrDefault();
                return handler;
            }
            set
            {
                HandlerId = value?.Id;
                LastQuery = null;
            }
        }


        private GameObject OnContent(Transform transform)
        {
            var asset = SearchSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/content.prefab");
            asset.SetActive(false);
            var content = Object.Instantiate(asset, transform);
            _comportment = content.GetComponent<SearchComponent>();
            _comportment.Initiate(this);
            _comportment.UpdateData();
            Logger.LogDebug($"SearchPage.OnGetContent: {asset.name} {content.name}");
            content.name = $"{GetKey()}_{content.name}";
            return content;
        }

        internal bool IsFetching = false;

        internal async UniTask Submit()
        {
            if (IsFetching) return;
            IsFetching = true;
            LastQuery = Query;
            _comportment.UpdateData();
            await UniTask.Yield();
            IsFetching = false;
            _comportment.UpdateData();
        }
    }
}