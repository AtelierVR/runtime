
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using Nox.CCK.Users;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
using Object = UnityEngine.Object;

namespace api.nox.game
{
    internal class NavigationTileManager
    {
        internal GameClientSystem clientMod;
        private TileObject tile;
        private HomeWidget _widget;
        private Dictionary<string, NavigationHandler> navigationHandlers = new();
        private EventSubscription navigationsub;
        internal UserNav usernav;
        internal WorldNav WorldNav;
        private string selectedHandler;

        internal NavigationTileManager(GameClientSystem clientMod)
        {
            this.clientMod = clientMod;
            navigationsub = clientMod.coreAPI.EventAPI.Subscribe("game.navigation", OnNavigationHandler);
            usernav = new UserNav(this);
            WorldNav = new WorldNav(this);
            GenerateWidget();
            Initialization().Forget();
        }

        private void OnNavigationHandler(EventData context)
        {
            var handler = (context.Data[0] as ShareObject).Convert<NavigationHandler>();
            if (handler == null) return;
            Debug.Log("Navigation handler received: " + handler.id);
            if (navigationHandlers.ContainsKey(handler.id) && handler.GetWorkers == null)
            {
                navigationHandlers.Remove(handler.id);
                if (tile != null) UpdateContent(tile.content);
                if (selectedHandler == handler.id)
                    OnSelectHandler(null);
                return;
            }
            if (handler.GetWorkers == null) return;
            if (navigationHandlers.ContainsKey(handler.id))
                navigationHandlers[handler.id] = handler;
            else navigationHandlers.Add(handler.id, handler);
        }

        private void OnSelectHandler(string id)
        {
            var tl = Reference.GetReference("tile.title", tile.content).GetComponent<TextLanguage>();
            if (selectedHandler == id) return;
            else if (id == null)
            {
                selectedHandler = null;
                tl.key = "dashboard.navigation.title";
                tl.UpdateText();
                foreach (Transform child in Reference.GetReference("resultats", tile.content).transform)
                    Object.Destroy(child.gameObject);
                return;
            }
            if (!navigationHandlers.ContainsKey(id)) return;
            var handler = navigationHandlers[id];
            tl.key = handler.title_key;
            tl.UpdateText();
            foreach (Transform child in Reference.GetReference("resultats", tile.content).transform)
                Object.Destroy(child.gameObject);
            selectedHandler = id;
        }

        private void GenerateWidget()
        {
            _widget = new HomeWidget
            {
                id = "api.nox.game.navigation",
                width = 1,
                height = 1,
                GetContent = (Transform tf) => GenerateWidgetContent(_widget, tf)
            };
        }

        private GameObject GenerateWidgetContent(HomeWidget data, Transform parent)
        {
            var baseprefab = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget");
            var prefab = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget.navigation");
            var btn = Object.Instantiate(baseprefab, parent);
            Object.Instantiate(prefab, Reference.GetReference("content", btn).transform);
            Reference.GetReference("button", btn).GetComponent<Button>().onClick.AddListener(OnClickWidget);
            return btn;
        }

        private void UpdateWidget()
        {
            if (_widget == null) return;
            clientMod.coreAPI.EventAPI.Emit("game.widget", _widget);
        }

        private void OnClickWidget() { clientMod.GotoTile("game.navigation"); }

        internal void OnDispose()
        {
            _widget.GetContent = null;
            UpdateWidget();
            _widget = null;
            usernav.OnDispose();
            clientMod.coreAPI.EventAPI.Unsubscribe(navigationsub);
            navigationHandlers = null;
        }

        internal void SendTile(EventData context)
        {
            if (this.tile != null)
            {
                clientMod.coreAPI.EventAPI.Emit("game.tile", this.tile);
                return;
            }
            var tile = new TileObject()
            {
                onRemove = () =>
                {
                    this.tile.content = null;
                    this.tile = null;
                }
            };
            var pf = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.navigation");
            tile.content = Object.Instantiate(pf);
            UpdateContent(tile.content);
            tile.id = "api.nox.game.navigation";
            this.tile = tile;
            if (navigationHandlers.Count > 0)
                OnSelectHandler(navigationHandlers.First().Value.id);

            Reference.GetReference("submit", tile.content).GetComponent<Button>().onClick
                .AddListener(() => SubmitSearch(Reference.GetReference("searchbar", tile.content).GetComponent<TMPro.TMP_InputField>().text));
            clientMod.coreAPI.EventAPI.Emit("game.tile", tile);
        }

        private List<CancellationTokenSource> IsFetching = new();
        private void SubmitSearch(string text)
        {
            if (selectedHandler == null) return;
            if (!navigationHandlers.ContainsKey(selectedHandler)) return;
            var handler = navigationHandlers[selectedHandler];
            if (handler.GetWorkers == null) return;
            Debug.Log("Submitting search to navigation handler: " + handler.id + " with text: " + text);
            var workers = handler.GetWorkers();
            if (workers.Length == 0) return;
            IsFetching = new List<CancellationTokenSource>();

            for (var i = 0; i < workers.Length; i++)
            {
                var worker = workers[i];
                if (worker == null) continue;
                var cancel = new CancellationTokenSource();
                IsFetching.Add(cancel);
                SearchResult(worker, text, new GameObject(), cancel).Forget();
            }
        }

        private async UniTask SearchResult(NavigationWorker worker, string text, GameObject obj, CancellationTokenSource cancel)
        {
            if (cancel.Token.IsCancellationRequested) return;
            var time = DateTime.Now;
            var result = worker.Fetch(text).AttachExternalCancellation(cancel.Token);
            await UniTask.WaitUntil(() => result.Status != UniTaskStatus.Pending || (DateTime.Now - time).TotalSeconds > 10);
            if (cancel.Token.IsCancellationRequested) return;
            if (result.Status != UniTaskStatus.Pending) cancel.Cancel();
            var foundobj = Reference.GetReference("found", obj);
            var messageobj = Reference.GetReference("message", obj);
            if (result.Status == UniTaskStatus.Faulted)
            {
                foundobj.SetActive(false);
                messageobj.SetActive(true);
                var msg = Reference.GetReference("text", messageobj).GetComponent<TextLanguage>();
                msg.key = "dashboard.navigation.error.unknown";
                msg.UpdateText();
                return;
            }
            else if (result.Status == UniTaskStatus.Canceled)
            {
                foundobj.SetActive(false);
                messageobj.SetActive(true);
                var msg = Reference.GetReference("text", messageobj).GetComponent<TextLanguage>();
                msg.key = "dashboard.navigation.error.timeout";
                msg.UpdateText();
                return;
            }
            var res = await result;
            if (res == null)
            {
                foundobj.SetActive(false);
                messageobj.SetActive(true);
                var msg = Reference.GetReference("text", messageobj).GetComponent<TextLanguage>();
                msg.key = "dashboard.navigation.error.unknown";
                msg.UpdateText();
                return;
            }
            else if (!string.IsNullOrEmpty(res.error))
            {
                foundobj.SetActive(false);
                messageobj.SetActive(true);
                var msg = Reference.GetReference("text", messageobj).GetComponent<TextLanguage>();
                msg.key = "dashboard.navigation.error";
                msg.arguments = new string[] { res.error };
                msg.UpdateText();
                return;
            }
            else if (res.data == null || res.data.Length == 0)
            {
                foundobj.SetActive(false);
                messageobj.SetActive(true);
                var msg = Reference.GetReference("text", messageobj).GetComponent<TextLanguage>();
                msg.key = "dashboard.navigation.error.empty";
                msg.UpdateText();
                return;
            }
            foundobj.SetActive(true);
            messageobj.SetActive(false);
            var pf = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/navigation.result");
            foreach (var data in res.data)
            {
                var go = Object.Instantiate(pf, Reference.GetReference("results", obj).transform);
                var title = Reference.GetReference("title", go).GetComponent<TextLanguage>();
                title.key = "dashboard.navigation.result.title";
                title.arguments = new string[] { data.title };
                title.UpdateText();
                // var button = Reference.GetReference("button", go).GetComponent<Button>();
                // ...
            }
        }



        private void UpdateContent(GameObject tile)
        {
            Debug.Log("Updating navigation tile");
            var searcher = Reference.GetReference("searcher", tile);
            foreach (Transform child in searcher.transform)
                Object.Destroy(child.gameObject);
            var pf = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/navigation.searcher");
            foreach (var handler in navigationHandlers.Values)
            {
                var id = handler.id;
                Debug.Log("Adding navigation handler to navigation tile" + handler.id);
                var go = Object.Instantiate(pf, searcher.transform);
                var button = Reference.GetReference("button", go).GetComponent<Button>();
                button.onClick.AddListener(() => OnSelectHandler(id));
                var title = Reference.GetReference("title", go).GetComponent<TextLanguage>();
                title.key = handler.text_key;
            }
        }


        private async UniTask Initialization()
        {
            await UniTask.DelayFrame(1);
            UpdateWidget();
        }
    }
}