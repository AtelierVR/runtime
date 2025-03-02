using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Events;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
using Object = UnityEngine.Object;
using Logger = Nox.CCK.Utils.Logger;
using api.nox.game.UI;
using Nox.CCK.Utils;
using Nox.CCK.Language;
using Transform = UnityEngine.Transform;

// ReSharper disable All

namespace api.nox.game.Tiles
{
    internal class NavigationTileManager : TileManager
    {
        internal Dictionary<string, NavigationHandler> NavigationHandlers = null;
        private EventSubscription _sub;
        internal UserNav UserNav;
        internal WorldNav WorldNav;
        internal ServerNav ServerNav;
        internal InstanceNav InstanceNav;
        private List<NavigationTile> tiles = new();

        internal NavigationTileManager()
        {
            _sub = GameClientSystem.CoreAPI.EventAPI.Subscribe("game.navigation", OnNavigationHandler);
            NavigationHandlers = new Dictionary<string, NavigationHandler>();
            UserNav = new UserNav();
            WorldNav = new WorldNav();
            ServerNav = new ServerNav();
            InstanceNav = new InstanceNav();
        }

        internal void PostInitialize()
        {
            Logger.Log("NavigationTileManager.PostInitialize");
            UserNav.UpdateHandler();
            WorldNav.UpdateHandler();
            ServerNav.UpdateHandler();
            InstanceNav.UpdateHandler();
        }

        private void OnNavigationHandler(EventData context)
        {
            if (context.Data[0] is not NavigationHandler handler) return;
            if (NavigationHandlers.ContainsKey(handler.id) && handler.GetWorkers == null)
            {
                NavigationHandlers.Remove(handler.id);

                foreach (var tile in tiles.Where(tile => tile != null))
                {
                    UpdateContent(tile, tile.content);
                    if (tile.SelectedHandler == handler.id)
                        OnSelectHandler(tile, tile.content, null);
                }

                return;
            }

            if (handler.GetWorkers == null) return;
            NavigationHandlers[handler.id] = handler;

            foreach (var tile in tiles.Where(tile => tile != null))
                UpdateContent(tile, tile.content);
        }

        private void OnSelectHandler(NavigationTile tile, GameObject content, string id)
        {
            var tl = Reference.GetReference("tile.title", content).GetComponent<TextLanguage>();
            if (tile.SelectedHandler == id) return;

            if (id == null)
            {
                tile.SelectedHandler = null;
                tl.UpdateText("dashboard.navigation.title");
                foreach (Transform child in Reference.GetReference("resultats", content).transform)
                    Object.Destroy(child.gameObject);
                return;
            }

            if (!NavigationHandlers.TryGetValue(id, out var handler)) return;
            tl.UpdateText(handler.title_key);
            foreach (Transform child in Reference.GetReference("resultats", content).transform)
                Object.Destroy(child.gameObject);
            tile.SelectedHandler = id;
        }

        internal void OnDispose()
        {
            foreach (var tile in tiles.ToArray())
                tile.onRemove();
            tiles.Clear();
            UserNav.OnDispose();
            WorldNav.OnDispose();
            ServerNav.OnDispose();
            InstanceNav.OnDispose();
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_sub);
            NavigationHandlers = null;
        }

        internal class NavigationTile : TileObject
        {
            public string SelectedHandler
            {
                get => GetData<string>(0);
                set => SetData(0, value);
            }

            internal readonly List<CancellationTokenSource> IsFetching = new();
        }

        internal void SendTile(EventData context)
        {
            var tile = new NavigationTile { id = "api.nox.game.navigation", context = context };
            tiles.Add(tile);
            tile.GetContent = (tf) => OnGetContent(tile, tf);
            tile.onDisplay = (str, gameObject) => OnDisplay(tile, gameObject);
            tile.onOpen = (str) => OnOpen(tile, tile.content);
            tile.onHide = (str) => OnHide(tile, tile.content);
            tile.onRemove = () => OnRemove(tile);
            MenuManager.Instance.SendTile(tile.MenuId, tile);
        }

        private void OnRemove(NavigationTile tile)
        {
            foreach (var c in tile.IsFetching) c.Cancel();
            tile.IsFetching.Clear();
            tiles.Remove(tile);
        }

        private GameObject OnGetContent(NavigationTile tile, Transform tf)
        {
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/navigation/content.prefab");
            pf.SetActive(false);
            tile.content = Object.Instantiate(pf, tf);
            tile.content.name = "api.nox.game.navigation";
            return tile.content;
        }

        private void OnDisplay(NavigationTile tile, GameObject content)
        {
            if (string.IsNullOrEmpty(tile.SelectedHandler) || !NavigationHandlers.ContainsKey(tile.SelectedHandler))
                OnSelectHandler(tile, content, NavigationHandlers.FirstOrDefault().Value?.id);
            UpdateContent(tile, content);

            var btn = Reference.GetReference("submit", content).GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SubmitSearch(
                tile, content,
                Reference.GetReference("searchbar", content).GetComponent<TMPro.TMP_InputField>().text
            ).Forget());
        }

        private void OnOpen(NavigationTile tile, GameObject content)
        {
            Logger.Log("NavigationTileManager.OnOpen");
            ForceUpdateLayout.UpdateManually(content);
        }

        private void OnHide(NavigationTile tile, GameObject content)
        {
            Logger.Log("NavigationTileManager.OnHide");
        }


        private async UniTask SubmitSearch(NavigationTile tile, GameObject content, string text)
        {
            if (tile.SelectedHandler == null) return;
            if (!NavigationHandlers.TryGetValue(tile.SelectedHandler, out var handler)) return;
            if (handler.GetWorkers == null) return;

            var workers = handler.GetWorkers();
            if (workers.Length == 0) return;

            foreach (var cancel in tile.IsFetching)
                cancel.Cancel();
            tile.IsFetching.Clear();

            var res = Reference.GetReference("resultats", content);
            foreach (Transform child in res.transform)
                Object.Destroy(child.gameObject);

            var pf = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/navigation/group.prefab");
            List<UniTask> tasks = new();

            responseSorter = 0;

            for (var i = 0; i < workers.Length; i++)
            {
                var worker = workers[i];
                if (worker == null) continue;
                var cancel = new CancellationTokenSource();
                tile.IsFetching.Add(cancel);
                var go = Object.Instantiate(pf, res.transform);
                go.name = "handler-" + i;

                Reference.GetReference("title", go)
                    .GetComponent<TextLanguage>()
                    .UpdateText(new[] { worker.server_title ?? worker.server_address });

                tasks.Add(SearchResult(tile, content, worker, text, go, cancel));
            }

            ForceUpdateLayout.UpdateManually(content);
            await UniTask.WhenAll(tasks);
            ForceUpdateLayout.UpdateManually(content);
        }

        private int responseSorter;

        private async UniTask SearchResult(
            NavigationTile tile, GameObject content,
            NavigationWorker worker, string query,
            GameObject obj, CancellationTokenSource cancel)
        {
            if (cancel.Token.IsCancellationRequested) return;

            var results = Reference.GetReference("results", obj);
            var message = Reference.GetReference("message", obj);
            var next = Reference.GetReference("next", obj).GetComponent<Button>();
            next.onClick.RemoveAllListeners();
            next.interactable = false;
            next.gameObject.SetActive(false);

            var text = Reference.GetReference("text", message).GetComponent<TextLanguage>();
            text.UpdateText("dashboard.navigation.loading");

            results.SetActive(false);
            message.SetActive(true);

            var result = worker.Fetch(query)
                .AttachExternalCancellation(cancel.Token);

            var time = DateTime.Now;
            await UniTask.WaitUntil(()
                => result.Status != UniTaskStatus.Pending
                   || (DateTime.Now - time).TotalSeconds > 10
            );

            if (cancel.Token.IsCancellationRequested || tile == null) return;
            if (result.Status != UniTaskStatus.Pending) cancel.Cancel();

            switch (result.Status)
            {
                case UniTaskStatus.Faulted:
                    try
                    {
                        await result;
                        text.UpdateText("dashboard.navigation.error.unknown");
                        ForceUpdateLayout.UpdateManually(content);
                        return;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex);
                        text.UpdateText("dashboard.navigation.error", new[] { ex.Message });
                        ForceUpdateLayout.UpdateManually(content);
                        return;
                    }
                case UniTaskStatus.Canceled or UniTaskStatus.Pending:
                {
                    text.UpdateText("dashboard.navigation.error.timeout");
                    ForceUpdateLayout.UpdateManually(content);
                    return;
                }
            }

            var res = await result;

            if (res == null)
            {
                text.UpdateText("dashboard.navigation.error.unknown");
                ForceUpdateLayout.UpdateManually(content);
                return;
            }

            if (!string.IsNullOrEmpty(res.error))
            {
                text.UpdateText("dashboard.navigation.error", new[] { res.error });
                ForceUpdateLayout.UpdateManually(content);
                return;
            }

            if (res.data == null || res.data.Length == 0)
            {
                text.UpdateText("dashboard.navigation.empty");
                ForceUpdateLayout.UpdateManually(content);
                return;
            }

            foreach (Transform child in results.transform)
                if (child != next.transform)
                    Object.Destroy(child.gameObject);

            results.SetActive(true);
            message.SetActive(false);

            foreach (var data in res.data)
                AppendResult(tile, results, data);

            obj.transform.SetSiblingIndex(responseSorter);
            responseSorter++;

            next.gameObject.SetActive(res.Next != null);
            if (next.gameObject.activeSelf)
            {
                next.interactable = true;
                next.transform.SetAsLastSibling();
                next.onClick.AddListener(() => NextElements(tile, content, obj, res).Forget());
            }

            ForceUpdateLayout.UpdateManually(obj);
            await UniTask.NextFrame();
            ForceUpdateLayout.UpdateManually(content);
        }

        private void AppendResult(NavigationTile tile, GameObject results, NavigationResultData data)
        {
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/navigation/result.prefab");
            var go = Object.Instantiate(pf, results.transform);
            go.name = "result-" + go.GetInstanceID();
            Reference.GetReference("title", go)
                .GetComponent<TextLanguage>()
                .UpdateText(new[] { data.title });
            var img = Reference.GetReference("image", go).GetComponent<RawImage>();
            img.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(data.imageUrl))
                _ = UpdateTexture(img, data.imageUrl).ContinueWith(_ =>
                {
                    img.gameObject.SetActive(true);
                    var rt = img.transform.parent.GetComponent<RectTransform>();
                    img.rectTransform.sizeDelta = img.texture.width < img.texture.height
                        ? new Vector2(rt.rect.width, rt.rect.width * img.texture.height / img.texture.width)
                        : new Vector2(rt.rect.height * img.texture.width / img.texture.height, rt.rect.height);
                });
            var button = Reference.GetReference("button", go).GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(GotoButton(tile, data));
        }

        private UnityEngine.Events.UnityAction GotoButton(NavigationTile tile, NavigationResultData data)
        {
            return () =>
            {
                foreach (var c in tile.IsFetching) c.Cancel();
                MenuManager.Instance.SendGotoTile(tile.MenuId, data.goto_id, data.goto_data);
            };
        }

        private async UniTask NextElements(NavigationTile tile, GameObject content, GameObject obj,
            NavigationResult res)
        {
            if (res.Next == null) return;

            var results = Reference.GetReference("results", obj);
            var next = Reference.GetReference("next", obj);
            var nextButton = Reference.GetReference("button", next).GetComponent<Button>();

            if (!nextButton.interactable) return;
            nextButton.interactable = false;
            nextButton.onClick.RemoveAllListeners();

            var cancel = new CancellationTokenSource();
            var result = res.Next()
                .AttachExternalCancellation(cancel.Token);
            tile.IsFetching.Add(cancel);

            var time = DateTime.Now;
            await UniTask.WaitUntil(()
                => result.Status != UniTaskStatus.Pending
                   || (DateTime.Now - time).TotalSeconds > 10);

            if (cancel.Token.IsCancellationRequested || tile == null) return;
            if (result.Status != UniTaskStatus.Pending) cancel.Cancel();

            switch (result.Status)
            {
                case UniTaskStatus.Faulted:
                    try
                    {
                        await result;
                        return;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex);
                        return;
                    }
                case UniTaskStatus.Canceled or UniTaskStatus.Pending:
                    return;
            }

            var newRes = await result;
            if (newRes == null) return;
            if (!string.IsNullOrEmpty(newRes.error)) return;
            if (newRes.data == null || newRes.data.Length == 0) return;

            foreach (var data in newRes.data)
                AppendResult(tile, results, data);

            nextButton.gameObject.SetActive(newRes.Next != null);
            if (nextButton.gameObject.activeSelf)
            {
                nextButton.interactable = true;
                nextButton.onClick.AddListener(() => NextElements(tile, content, obj, newRes).Forget());
            }

            ForceUpdateLayout.UpdateManually(content);
        }

        private void UpdateContent(NavigationTile tile, GameObject content)
        {
            var searcher = Reference.GetReference("searcher", content);
            foreach (Transform child in searcher.transform)
                Object.Destroy(child.gameObject);
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/navigation/list.prefab");
            foreach (var handler in NavigationHandlers.Values)
            {
                var id = handler.id;
                var go = Object.Instantiate(pf, searcher.transform);
                Reference.GetReference("button", go)
                    .GetComponent<Button>()
                    .onClick.AddListener(() => OnSelectHandler(tile, content, id));

                var no_icon = Reference.GetReference("no_icon", go);
                var with_icon = Reference.GetReference("with_icon", go);

                if (handler.icon)
                {
                    no_icon.SetActive(false);
                    with_icon.SetActive(true);
                    Reference.GetReference("icon", with_icon).GetComponent<RawImage>().texture = handler.icon;
                    Reference.GetReference("text", with_icon).GetComponent<TextLanguage>()
                        .UpdateText(handler.text_key);
                }
                else
                {
                    no_icon.SetActive(true);
                    with_icon.SetActive(false);
                    Reference.GetReference("text", no_icon).GetComponent<TextLanguage>()
                        .UpdateText(handler.text_key);
                }
            }
        }
    }
}