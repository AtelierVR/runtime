
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
        internal UserNav UserNav;
        internal WorldNav WorldNav;
        internal ServerNav ServerNav;
        internal InstanceNav InstanceNav;
        private string selectedHandler;

        internal NavigationTileManager(GameClientSystem clientMod)
        {
            this.clientMod = clientMod;
            navigationsub = clientMod.coreAPI.EventAPI.Subscribe("game.navigation", OnNavigationHandler);
            UserNav = new UserNav(this);
            WorldNav = new WorldNav(this);
            ServerNav = new ServerNav(this);
            InstanceNav = new InstanceNav(this);
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
            UserNav.OnDispose();
            WorldNav.OnDispose();
            ServerNav.OnDispose();
            InstanceNav.OnDispose();
            clientMod.coreAPI.EventAPI.Unsubscribe(navigationsub);
            navigationHandlers = null;
        }

        internal void SendTile(EventData context)
        {
            string tt = null;
            var tile = new TileObject()
            {
                onHide = (string id) => tt = id,
                onRemove = () => { if (tt == this.tile.id) return; this.tile = null; },
                onDisplay = (string id) => tt = null
            };
            var pf = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.navigation");
            pf.SetActive(false);
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
            var pf = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/navigation.container");
            var res = Reference.GetReference("resultats", tile.content);
            foreach (Transform child in res.transform)
                Object.Destroy(child.gameObject);
            for (var i = 0; i < workers.Length; i++)
            {
                var worker = workers[i];
                if (worker == null) continue;
                var cancel = new CancellationTokenSource();
                IsFetching.Add(cancel);
                var go = Object.Instantiate(pf, res.transform);
                go.name = "handler-" + i;
                var ct = Reference.GetReference("title", go).GetComponent<TextLanguage>();
                ct.arguments = new string[] { worker.server_title ?? worker.server_address };
                ct.UpdateText();
                SearchResult(worker, text, go, cancel).Forget();
            }
            ForceUpdateLayout.UpdateManually(tile.content);
        }

        private async UniTask SearchResult(NavigationWorker worker, string text, GameObject obj, CancellationTokenSource cancel)
        {
            if (cancel.Token.IsCancellationRequested) return;
            var time = DateTime.Now;
            var result = worker.Fetch(text).AttachExternalCancellation(cancel.Token);
            await UniTask.WaitUntil(() => result.Status != UniTaskStatus.Pending || (DateTime.Now - time).TotalSeconds > 10);
            Debug.Log("Search result for " + worker.server_title + " in " + (DateTime.Now - time).TotalSeconds + " seconds");
            if (cancel.Token.IsCancellationRequested) return;
            if (result.Status != UniTaskStatus.Pending) cancel.Cancel();
            var foundobj = Reference.GetReference("found", obj);
            var messageobj = Reference.GetReference("message", obj);
            Debug.Log("Search result for " + worker.server_title + " status: " + result.Status);
            if (result.Status == UniTaskStatus.Faulted)
            {
                try
                {
                    await result;
                    foundobj.SetActive(false);
                    messageobj.SetActive(true);
                    var msg = Reference.GetReference("text", messageobj).GetComponent<TextLanguage>();
                    msg.key = "dashboard.navigation.error.unknown";
                    msg.UpdateText();
                    ForceUpdateLayout.UpdateManually(tile.content);
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    foundobj.SetActive(false);
                    messageobj.SetActive(true);
                    var msg = Reference.GetReference("text", messageobj).GetComponent<TextLanguage>();
                    msg.key = "dashboard.navigation.error";
                    msg.arguments = new string[] { ex.Message };
                    msg.UpdateText();
                    ForceUpdateLayout.UpdateManually(tile.content);
                    return;
                }
            }
            else if (result.Status == UniTaskStatus.Canceled || result.Status == UniTaskStatus.Pending)
            {
                foundobj.SetActive(false);
                messageobj.SetActive(true);
                var msg = Reference.GetReference("text", messageobj).GetComponent<TextLanguage>();
                msg.key = "dashboard.navigation.error.timeout";
                msg.UpdateText();
                ForceUpdateLayout.UpdateManually(tile.content);
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
                ForceUpdateLayout.UpdateManually(tile.content);
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
                ForceUpdateLayout.UpdateManually(tile.content);
                return;
            }
            else if (res.data == null || res.data.Length == 0)
            {
                foundobj.SetActive(false);
                messageobj.SetActive(true);
                var msg = Reference.GetReference("text", messageobj).GetComponent<TextLanguage>();
                msg.key = "dashboard.navigation.empty";
                msg.UpdateText();
                ForceUpdateLayout.UpdateManually(tile.content);
                return;
            }
            foundobj.SetActive(true);
            messageobj.SetActive(false);
            var pf = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/navigation.result");
            var refs = Reference.GetReference("results", obj);
            foreach (Transform child in refs.transform)
                Object.Destroy(child.gameObject);
            for (var i = 0; i < res.data.Length; i++)
            {
                var data = res.data[i];
                var go = Object.Instantiate(pf, refs.transform);
                go.name = "result-" + i;
                var title = Reference.GetReference("title", go).GetComponent<TextLanguage>();
                title.arguments = new string[] { data.title };
                title.UpdateText();
                var img = Reference.GetReference("image", go).GetComponent<RawImage>();
                img.gameObject.SetActive(false);
                if (!string.IsNullOrEmpty(data.imageUrl))
                {
                    UniTask uniTask = UpdateTexure(img, data.imageUrl).ContinueWith(_ =>
                    {
                        img.gameObject.SetActive(true);
                        var rt = img.transform.parent.GetComponent<RectTransform>();
                        img.rectTransform.sizeDelta = img.texture.width < img.texture.height ?
                            new Vector2(rt.rect.width, rt.rect.width * img.texture.height / img.texture.width) :
                            new Vector2(rt.rect.height * img.texture.width / img.texture.height, rt.rect.height);
                    });
                }
                var button = Reference.GetReference("button", go).GetComponent<Button>();
                button.onClick.AddListener(() => clientMod.GotoTile(data.goto_id, data.goto_data));
            }
            ForceUpdateLayout.UpdateManually(tile.content);
        }


        private async UniTask<bool> UpdateTexure(RawImage img, string url)
        {
            var tex = await clientMod.coreAPI.NetworkAPI.FetchTexture(url);
            if (tex != null)
            {
                img.texture = tex;
                return true;
            }
            else return false;
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