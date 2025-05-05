using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace api.nox.search.client
{
    public class SearchComponent : MonoBehaviour, IUpdateLayout
    {
        public TMPro.TMP_InputField queryInput;
        public TextLanguage queryPlaceholder;
        public RawImage searchIcon;

        public GameObject searchContainer;
        public Button searchButton;
        public GameObject refreshContainer;
        public Button refreshButton;
        public GameObject cancelContainer;
        public Button cancelButton;

        public Button menuButton;
        public TextLanguage resultText;
        public RectTransform workerContainer;

        public Button backButton;
        public Button closeButton;
        public RectTransform handlerContainer;
        public TextLanguage handlerText;
        public RectTransform menuContainer;
        public Button menuBackgroundCloseButton;
        public RectTransform menuNavContainer;
        public float menuMaxWidth = 750f;

        internal SearchPage Page;

        public void UpdateLayout()
        {
            if (!menuNavContainer.gameObject.activeSelf) return;
            var size = menuNavContainer.sizeDelta;
            size.x = Mathf.Min(menuContainer.rect.width, menuMaxWidth);
            menuNavContainer.sizeDelta = size;
        }

        private void OnTransformParentChanged()
            => UpdateLayout();

        private void OnMenuClick()
        {
            menuContainer.gameObject.SetActive(true);
            UpdateLayout();
        }

        private void OnMenuClose()
            => menuContainer.gameObject.SetActive(false);

        private void OnDestroy()
        {
            queryInput.onValueChanged.RemoveListener(OnQueryChanged);
            searchButton.onClick.RemoveListener(OnSubmit);
            queryInput.onSubmit.RemoveListener(OnSubmit);
            refreshButton.onClick.RemoveListener(OnSubmit);
            cancelButton.onClick.RemoveListener(OnCancel);
            backButton.onClick.RemoveListener(OnBack);
            closeButton.onClick.RemoveListener(OnMenuClose);
            menuButton.onClick.RemoveListener(OnMenuClick);
            menuBackgroundCloseButton.onClick.RemoveListener(OnMenuClose);

            if (Page == null) return;
            Page.OnWorkerTaskUpdate.RemoveListener(OnWorkerTaskUpdate);
            Page.OnWorkerTaskStart.RemoveListener(OnWorkerTaskStart);
            Page.OnHandlerUpdate.RemoveListener(UpdateHandler);
        }

        private void OnBack()
            => SearchSystem.CoreAPI.EventAPI
                .Emit("goto_action", Page.MenuId, "back");

        internal void Initiate(SearchPage page)
        {
            if (Page != null) return;
            Page = page;
            Page.OnWorkerTaskUpdate.AddListener(OnWorkerTaskUpdate);
            Page.OnWorkerTaskStart.AddListener(OnWorkerTaskStart);
            Page.OnHandlerUpdate.AddListener(UpdateHandler);
            OnMenuClose();
        }

        private void Awake()
        {
            if (Page == null)
            {
                Logger.LogError("SearchComponent: SearchPage is not initiated.");
                return;
            }

            queryInput.onValueChanged.AddListener(OnQueryChanged);
            searchButton.onClick.AddListener(OnSubmit);
            queryInput.onSubmit.AddListener(OnSubmit);
            refreshButton.onClick.AddListener(OnSubmit);
            cancelButton.onClick.AddListener(OnCancel);
            backButton.onClick.AddListener(OnBack);
            closeButton.onClick.AddListener(OnMenuClose);
            menuButton.onClick.AddListener(OnMenuClick);
            menuBackgroundCloseButton.onClick.AddListener(OnMenuClose);
        }

        private void OnWorkerTaskStart(SearchPage.WorkerTask[] tasks)
        {
            foreach (Transform tf in workerContainer)
                Destroy(tf.gameObject);

            if (tasks.Length == 0)
            {
                resultText.UpdateText("search.no_worker");
                resultText.gameObject.SetActive(true);
                workerContainer.gameObject.SetActive(false);
                return;
            }

            foreach (var task in tasks)
            {
                var asset = SearchSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/worker.prefab");
                asset.SetActive(false);
                var content = Instantiate(asset, workerContainer);
                var component = content.GetComponent<WorkerComponent>();
                component.Initiate(this, task);
                content.name = $"{task.Uid}_{content.name}";
                content.SetActive(true);
            }

            resultText.gameObject.SetActive(false);
            workerContainer.gameObject.SetActive(true);
            ForceUpdateLayout.UpdateManually(workerContainer);
            UpdateSearchButtons();
        }

        private void OnWorkerTaskUpdate(SearchPage.WorkerTask task)
        {
            foreach (Transform tf in workerContainer)
            {
                var component = tf.GetComponent<WorkerComponent>();
                if (!component || component.Task.Uid != task.Uid) continue;
                component.UpdateData(task);
            }

            ForceUpdateLayout.UpdateManually(workerContainer);
            UpdateSearchButtons();
        }


        private void OnSubmit(string query)
        {
            OnQueryChanged(query);
            OnSubmit();
            UpdateSearchButtons();
        }

        private void OnQueryChanged(string query)
        {
            Page.Query = query.Trim();
            UpdateSearchButtons();
        }

        private void UpdateSearchButtons()
        {
            if (Page.IsFetching)
            {
                cancelContainer.SetActive(true);
                searchContainer.SetActive(false);
                refreshContainer.SetActive(false);
            }
            else if (Page.IsNewQuery)
            {
                searchContainer.SetActive(true);
                cancelContainer.SetActive(false);
                refreshContainer.SetActive(false);
            }
            else
            {
                refreshContainer.SetActive(true);
                cancelContainer.SetActive(false);
                searchContainer.SetActive(false);
            }
        }

        private void OnSubmit() => Page?.Submit().Forget();
        private void OnCancel() => Page?.Cancel();


        public void UpdateData()
        {
            queryInput.text = Page.Query.Trim();
            UpdateSearchButtons();
            var handler = Page.Handler;
            queryPlaceholder.UpdateText(string.IsNullOrEmpty(handler.PlaceholderKey)
                ? "search.query.placeholder"
                : handler.PlaceholderKey);
            searchIcon.texture = !handler.Icon
                ? SearchSystem.CoreAPI.AssetAPI.GetAsset<Texture2D>("ui", "icons/apps.png")
                : handler.Icon;
            UpdateHandler(SearchSystem.Instance.Handlers.ToArray());
        }

        private void UpdateHandler(Handler[] handlers)
        {
            if (handlers.Length == 0)
            {
                handlerText.UpdateText("search.no_handler");
                handlerText.gameObject.SetActive(true);
                handlerContainer.gameObject.SetActive(false);
                return;
            }

            handlerContainer.gameObject.SetActive(true);
            handlerText.gameObject.SetActive(false);

            var ids = new List<string>();
            foreach (Transform tf in handlerContainer)
            {
                var component = tf.GetComponent<HandlerComponent>();
                if (!component)
                {
                    Destroy(tf.gameObject);
                    continue;
                }

                var handler = Array.Find(handlers, h => h.Id == component.Data.Id);
                if (handler == null)
                {
                    Destroy(tf.gameObject);
                    continue;
                }

                ids.Add(handler.Id);
                component.UpdateData(handler);
            }

            foreach (var handler in handlers)
            {
                if (ids.Contains(handler.Id)) continue;
                var asset = SearchSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/handler.prefab");
                asset.SetActive(false);
                var content = Instantiate(asset, handlerContainer);
                var component = content.GetComponent<HandlerComponent>();
                component.Initiate(handler);
                component.button.onClick.AddListener(() => OnChangeHandler(handler));
                content.name = $"{handler.Id}_{content.name}";
                content.SetActive(true);
                ids.Add(handler.Id);
            }


            foreach (Transform tf in handlerContainer)
            {
                var component = tf.GetComponent<HandlerComponent>();
                if (!component) continue;
                component.button.interactable = component.Data.Id != Page.Handler.Id;
            }

            ForceUpdateLayout.UpdateManually(handlerContainer);
        }

        private void OnChangeHandler(Handler handler)
        {
            if (Page == null)
            {
                Logger.LogError("SearchComponent: SearchPage is null.");
                return;
            }

            if (handler == null)
            {
                Logger.LogError("SearchComponent: Handler is null.");
                return;
            }

            if (Page.Handler == handler) return;
            Page.Handler = handler;
            UpdateData();
        }
    }
}