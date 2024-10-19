
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
using Object = UnityEngine.Object;
using api.nox.game.UI;

namespace api.nox.game.Tiles
{
    internal class NavigationTileManager : TileManager
    {
        internal Dictionary<string, NavigationHandler> navigationHandlers = new();
        private EventSubscription _sub;
        internal UserNav UserNav;
        internal WorldNav WorldNav;
        internal ServerNav ServerNav;
        internal InstanceNav InstanceNav;
        private string selectedHandler;

        internal NavigationTileManager()
        {
            _sub = GameClientSystem.CoreAPI.EventAPI.Subscribe("game.navigation", OnNavigationHandler);
            UserNav = new UserNav(this);
            WorldNav = new WorldNav(this);
            ServerNav = new ServerNav(this);
            InstanceNav = new InstanceNav(this);
        }

        private void OnNavigationHandler(EventData context)
        {
            if (context.Data[0] is not NavigationHandler handler) return;
            if (navigationHandlers.ContainsKey(handler.id) && handler.GetWorkers == null)
            {
                navigationHandlers.Remove(handler.id);
                // // if (tile != null) UpdateContent(tile);
                // if (selectedHandler == handler.id)
                //     OnSelectHandler(null, null, null);
                return;
            }
            if (handler.GetWorkers == null) return;
            if (navigationHandlers.ContainsKey(handler.id))
                navigationHandlers[handler.id] = handler;
            else navigationHandlers.Add(handler.id, handler);
        }

        private void OnSelectHandler(TileObject tile, GameObject content, string id)
        {
            var tl = Reference.GetReference("tile.title", content).GetComponent<TextLanguage>();
            if (selectedHandler == id) return;
            else if (id == null)
            {
                selectedHandler = null;
                tl.UpdateText("dashboard.navigation.title");
                foreach (Transform child in Reference.GetReference("resultats", content).transform)
                    Object.Destroy(child.gameObject);
                return;
            }
            if (!navigationHandlers.ContainsKey(id)) return;
            var handler = navigationHandlers[id];
            tl.UpdateText(handler.title_key);
            foreach (Transform child in Reference.GetReference("resultats", content).transform)
                Object.Destroy(child.gameObject);
            selectedHandler = id;
        }

        internal void OnDispose()
        {
            UserNav.OnDispose();
            WorldNav.OnDispose();
            ServerNav.OnDispose();
            InstanceNav.OnDispose();
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_sub);
            navigationHandlers = null;
        }

        internal void SendTile(EventData context)
        {
            var tile = new TileObject() { id = "api.nox.game.navigation", context = context };
            tile.GetContent = (Transform tf) => OnGetContent(tile, tf);
            tile.onDisplay = (str, gameObject) => OnDisplay(tile, gameObject);
            tile.onOpen = (str) => OnOpen(tile, tile.content);
            tile.onHide = (str) => OnHide(tile, tile.content);
            MenuManager.Instance.SendTile(tile.MenuId, tile);
        }

        internal GameObject OnGetContent(TileObject tile, Transform tf)
        {
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.navigation");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = "api.nox.game.navigation";
            return content;
        }

        internal void OnDisplay(TileObject tile, GameObject content)
        {
            Debug.Log("NavigationTileManager.OnDisplay");
            if (navigationHandlers.Count > 0)
                OnSelectHandler(tile, content, navigationHandlers.First().Value.id);
            UpdateContent(tile, content);
            var btn = Reference.GetReference("submit", content).GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SubmitSearch(
                tile, content,
                Reference.GetReference("searchbar", content).GetComponent<TMPro.TMP_InputField>().text
            ));
        }

        internal void OnOpen(TileObject tile, GameObject content)
        {
            Debug.Log("NavigationTileManager.OnOpen");
        }

        internal void OnHide(TileObject tile, GameObject content)
        {
            Debug.Log("NavigationTileManager.OnHide");
        }

        private List<CancellationTokenSource> IsFetching = new();

        private void SubmitSearch(TileObject tile, GameObject content, string text)
        {
            Debug.Log($"NavigationTileManager.SubmitSearch({selectedHandler}, {text})");
            if (selectedHandler == null) return;
            if (!navigationHandlers.ContainsKey(selectedHandler)) return;
            var handler = navigationHandlers[selectedHandler];
            if (handler.GetWorkers == null) return;
            Debug.Log("Submitting search to navigation handler: " + handler.id + " with text: " + text);
            var workers = handler.GetWorkers();
            if (workers.Length == 0) return;
            IsFetching = new List<CancellationTokenSource>();
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/navigation.container");
            var res = Reference.GetReference("resultats", content);
            foreach(var c in IsFetching)
                c.Cancel();
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
                SearchResult(tile, content, worker, text, go, cancel).Forget();
            }
            ForceUpdateLayout.UpdateManually(content);
        }

        private async UniTask SearchResult(TileObject tile, GameObject content, NavigationWorker worker, string text, GameObject obj, CancellationTokenSource cancel)
        {
            if (cancel.Token.IsCancellationRequested) return;
            var time = DateTime.Now;
            var result = worker.Fetch(text).AttachExternalCancellation(cancel.Token);
            await UniTask.WaitUntil(() => result.Status != UniTaskStatus.Pending || (DateTime.Now - time).TotalSeconds > 10);
            Debug.Log("Search result for " + worker.server_title + " in " + (DateTime.Now - time).TotalSeconds + " seconds");
            if (cancel.Token.IsCancellationRequested || tile == null) return;
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
                    ForceUpdateLayout.UpdateManually(content);
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
                    ForceUpdateLayout.UpdateManually(content);
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
                ForceUpdateLayout.UpdateManually(content);
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
                ForceUpdateLayout.UpdateManually(content);
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
                ForceUpdateLayout.UpdateManually(content);
                return;
            }
            else if (res.data == null || res.data.Length == 0)
            {
                foundobj.SetActive(false);
                messageobj.SetActive(true);
                var msg = Reference.GetReference("text", messageobj).GetComponent<TextLanguage>();
                msg.key = "dashboard.navigation.empty";
                msg.UpdateText();
                ForceUpdateLayout.UpdateManually(content);
                return;
            }
            foundobj.SetActive(true);
            messageobj.SetActive(false);
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/navigation.result");
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
                    _ = UpdateTexure(img, data.imageUrl).ContinueWith(_ =>
                    {
                        img.gameObject.SetActive(true);
                        var rt = img.transform.parent.GetComponent<RectTransform>();
                        img.rectTransform.sizeDelta = img.texture.width < img.texture.height ?
                            new Vector2(rt.rect.width, rt.rect.width * img.texture.height / img.texture.width) :
                            new Vector2(rt.rect.height * img.texture.width / img.texture.height, rt.rect.height);
                    });
                var button = Reference.GetReference("button", go).GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    Debug.Log($"Sending goto tile {tile.MenuId} with {data.goto_id}");
                    foreach (var c in IsFetching)
                        c.Cancel();
                    for (int i = 0; i < data.goto_data.Length; i++)
                        Debug.Log($"data.goto_data[{i}]: {data.goto_data[i]}");
                    MenuManager.Instance.SendGotoTile(tile.MenuId, data.goto_id, data.goto_data);
                });
            }
            ForceUpdateLayout.UpdateManually(content);
        }



        private void UpdateContent(TileObject tile, GameObject content)
        {
            Debug.Log("Updating navigation tile");
            var searcher = Reference.GetReference("searcher", content);
            foreach (Transform child in searcher.transform)
                Object.Destroy(child.gameObject);
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/navigation.searcher");
            foreach (var handler in navigationHandlers.Values)
            {
                var id = handler.id;
                Debug.Log("Adding navigation handler to navigation tile" + handler.id);
                var go = Object.Instantiate(pf, searcher.transform);
                var button = Reference.GetReference("button", go).GetComponent<Button>();
                button.onClick.AddListener(() => OnSelectHandler(tile, content, id));
                var title = Reference.GetReference("title", go).GetComponent<TextLanguage>();
                title.key = handler.text_key;
            }
        }
    }
}