using System;
using Nox.CCK;
using Nox.CCK.Mods;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace api.nox.game.UI
{
    public class Menu : MonoBehaviour, ShareObject, IDisposable
    {
        public int Id => GetInstanceID();
        public string initTile = "default";
        public NavigationMenu[] navigationMenus;
        public RectTransform container;

        internal MenuHistory History;

        public virtual bool IsVisible
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }

        void Awake()
        {
            if (container == null)
                container = GetComponent<RectTransform>();
            History = new MenuHistory(this);
        }

        void Start() => UpdateMenu();
        void OnValidate() => UpdateMenu();
        public virtual void Dispose() { }


        /// <summary>
        /// Update the menu
        /// </summary>
        void UpdateMenu()
        {
#if UNITY_EDITOR
            if (EditorUtility.IsPersistent(gameObject)) return;
#endif
            foreach (var nav in navigationMenus)
            {
                UpdateContent(nav);
                UpdatePosition(nav.container, nav.anchor);
            }
        }


        /// <summary>
        /// Update the position of the navigation menu
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="direction"></param>
        private void UpdatePosition(RectTransform menu, Vector2 direction)
        {
            // bottom center
            if (direction == Vector2.down)
            {
                menu.anchorMin = new Vector2(0.5f, 0);
                menu.anchorMax = new Vector2(0.5f, 0);
                menu.pivot = new Vector2(0.5f, 0);
                menu.anchoredPosition = new Vector2(0, 0);
            }
            // bottom left
            else if (direction == Vector2.left + Vector2.down)
            {
                menu.anchorMin = new Vector2(0, 0);
                menu.anchorMax = new Vector2(0, 0);
                menu.pivot = new Vector2(0, 0);
                menu.anchoredPosition = new Vector2(0, 0);
            }
            // bottom right
            else if (direction == Vector2.right + Vector2.down)
            {
                menu.anchorMin = new Vector2(1, 0);
                menu.anchorMax = new Vector2(1, 0);
                menu.pivot = new Vector2(1, 0);
                menu.anchoredPosition = new Vector2(0, 0);
            }
        }


        /// <summary>
        /// Update the content of the navigation menu
        /// </summary>
        /// <param name="nav"></param>
        private void UpdateContent(NavigationMenu nav)
        {
            if (nav.items.Length == 0)
            {
                nav.container.gameObject.SetActive(false);
                return;
            }
            else nav.container.gameObject.SetActive(true);
            for (int i = nav.items.Length; i < nav.container.childCount; i++)
#if UNITY_EDITOR
                if (Application.isPlaying)
                    Destroy(nav.container.GetChild(i).gameObject);
                else
                {
                    var menuItem = nav.container.GetChild(i);
                    menuItem.gameObject.SetActive(false);
                    menuItem.name = $"menu-item-{i}-hidden";
                }
#else
                Destroy(nav.container.GetChild(i).gameObject);
#endif
            for (int i = 0; i < nav.items.Length; i++)
            {
                var item = nav.items[i];
                var menuItem = nav.container.childCount > i
                    ? nav.container.GetChild(i)
                    : Instantiate(nav.itemPrefab, nav.container).transform;
                menuItem.name = $"menu-item-{i}-{item.key}";
                menuItem.GetComponentInChildren<RawImage>().texture = item.icon;
                menuItem.GetComponentInChildren<TextLanguage>()?.UpdateText(item.text);
                var btn = menuItem.GetComponent<Button>();
                btn.interactable = item.flags.HasFlag(NavigationMenuItemActiveFlags.Interactable);
                menuItem.gameObject.SetActive(item.flags.HasFlag(NavigationMenuItemActiveFlags.Enabled));
                btn.onClick.RemoveAllListeners();
                if (item.execution_type != NavigationMenuItemExecutionEnum.None)
                    btn.onClick.AddListener(() => OnNavifationClick(item));
            }
        }

        private void OnNavifationClick(NavigationMenuItem item)
        {
            switch (item.execution_type)
            {
                case NavigationMenuItemExecutionEnum.GotoTile:
                    MenuManager.Instance.SendGotoTile(Id, item.execution, item.execution_arguments);
                    break;
                case NavigationMenuItemExecutionEnum.SendEvent:
                    GameClientSystem.CoreAPI.EventAPI.Emit(item.execution, item.execution_arguments);
                    break;
                case NavigationMenuItemExecutionEnum.MenuAction:
                    OnActionExecuted(item.execution, item.execution_arguments);
                    break;
                default:
                    Debug.LogWarning("Unknown execution type");
                    break;
            }
        }

        private void OnActionExecuted(string action, object[] args)
        {
            if (action == "history.backward")
                History.GoBack();
            else if (action == "history.forward")
                History.GoForward();
            else if (action == "history.restore")
                History.Restore();
            else if (action == "history.clear")
                History.Clear();
            else if (action == "menu.close")
                IsVisible = false;
            else if (action == "menu.open")
                IsVisible = true;
            else if (action == "menu.toggle")
                IsVisible = !IsVisible;
            else Debug.LogWarning("Unknown action");
        }

        public void SetTile(TileObject tile, TileObject oldTile = null, SetTileFlags flags = SetTileFlags.None)
        {
            if (tile.content == null && tile.GetContent == null)
                throw new Exception("Tile content is null");

            if (oldTile != null)
            {
                Debug.Log($"Hiding old tile {oldTile.id}");
                oldTile.onHide?.DynamicInvoke(tile.id);
                oldTile.content.SetActive(false);
            }

            if (tile != null)
            {
                if (tile.content == null)
                    tile.content = tile.GetContent(container);
                if (flags.HasFlag(SetTileFlags.IsNew))
                {
                    Debug.Log($"Opening new tile {tile.id}");
                    tile.onOpen?.DynamicInvoke(oldTile?.id);
                }
                if (flags.HasFlag(SetTileFlags.IsRestore))
                {
                    Debug.Log($"Restoring tile {tile.id}");
                    tile.onRestore?.DynamicInvoke(oldTile?.id);
                }
                Debug.Log($"Displaying tile {tile.id}");
                tile.onDisplay?.DynamicInvoke(oldTile?.id, tile.content);
                tile.content.name = tile.id;
                tile.content.SetActive(true);
                ForceUpdateLayout.UpdateManually(tile.content.GetComponent<RectTransform>());
            }
            else Debug.LogWarning("No tile to display");
        }
    }

    public enum SetTileFlags : byte
    {
        None = 0,
        IsNew = 1,
        IsRestore = 2,
        IsBack = 4,
        IsForward = 8
    }
}