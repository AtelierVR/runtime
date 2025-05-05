using System;
using api.nox.ui.histories;
using api.nox.ui.pages;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.ui.menus
{
    public abstract class Menu : MonoBehaviour, INoxObject, IDisposable
    {
        [NoxPublic(NoxAccess.Method)]
        public int GetId() => GetInstanceID();

        [NoxPublic(NoxAccess.Read)]
        public readonly HistoryList History = new();

        public RectTransform container;

        [NoxPublic(NoxAccess.Method)]
        public virtual void Show() => gameObject.SetActive(true);

        [NoxPublic(NoxAccess.Method)]
        public virtual void Hide() => gameObject.SetActive(false);

        [NoxPublic(NoxAccess.Method)]
        public virtual bool IsVisible() => gameObject.activeSelf;
        
        
        [NoxPublic(NoxAccess.Method)]
        public void Goto(string pageKey, object[] args = null)
            => UISystem.CoreAPI.EventAPI.Emit("goto_page", GetId(), pageKey, args ?? Array.Empty<object>());

        public void SetPage(Page newPage, Page oldPage = null, PageFlags flags = PageFlags.None)
        {
            try
            {
                if (newPage == null || !newPage.Content && newPage.GetContent == null)
                    return;

                if (oldPage != null)
                {
                    Logger.Log($"Hiding old tile {oldPage.Key}");
                    oldPage.OnHide?.Invoke(newPage.Key, oldPage.Content);
                    oldPage.Content?.SetActive(false);
                }

                if (!newPage.Content)
                    newPage.Content = newPage.GetContent(container);

                if (flags.HasFlag(PageFlags.IsNew))
                {
                    Logger.Log($"Opening new tile {newPage.Key}");
                    newPage.OnOpen?.Invoke(oldPage?.Key, newPage.Content);
                }

                if (flags.HasFlag(PageFlags.IsRestore))
                {
                    Logger.Log($"Restoring tile {newPage.Key}");
                    newPage.OnRestore?.Invoke(oldPage?.Key, newPage.Content);
                }

                Logger.Log($"Displaying tile {newPage.Key}");
                newPage.OnDisplay?.Invoke(oldPage?.Key, newPage.Content);
                newPage.Content.name = newPage.Key;
                newPage.Content.SetActive(true);

                ForceUpdateLayout.UpdateManually(newPage.Content);
            }
            catch (Exception e)
            {
                Logger.LogWarning("Error setting tile");
                Logger.LogError(e);
            }
        }

        private Vector2 _lastSize;
        public void Update()
        {
            if (!container) return;
            if (container.rect.size == _lastSize) return;
            _lastSize = container.rect.size;
            ForceUpdateLayout.UpdateManually(container);
        }

        public void Dispose()
        {
            Hide();
            History.Clear(this);
            foreach (UnityEngine.Transform child in container)
                Destroy(child.gameObject);
        }
        
    }
}