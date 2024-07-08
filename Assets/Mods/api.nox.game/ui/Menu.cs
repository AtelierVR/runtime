using System;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.game
{
    public class Menu : MonoBehaviour
    {
        [Header("Menu bars")]
        public GameObject menuItemPrefab;
        public RectTransform centerMenu;
        public RectTransform leftMenu;
        public RectTransform rightMenu;
        [Header("Content", order = 2)]
        public MenuItem[] bottomCenterItems;
        public MenuItem[] bottomLeftItems;
        public MenuItem[] bottomRightItems;

        [Header("Notification bar")]
        public GameObject notificationItemPrefab;
        public RectTransform notificationMenu;
        [Header("Content", order = 2)]
        public MenuItem[] notificationItems;

        [Header("Container")]
        public RectTransform container;

        void Start() => UpdateMenu();

        void UpdateMenu()
        {
#if UNITY_EDITOR
            if (EditorUtility.IsPersistent(gameObject)) return;
#endif
            UpdateContent(centerMenu, bottomCenterItems);
            UpdateContent(leftMenu, bottomLeftItems);
            UpdateContent(rightMenu, bottomRightItems);

            UpdatePosition(centerMenu, Vector2.down);
            UpdatePosition(leftMenu, Vector2.left + Vector2.down);
            UpdatePosition(rightMenu, Vector2.right + Vector2.down);

            UpdateNotification();
        }

        private void UpdateNotification()
        {
            if (notificationMenu == null) return;
            if (notificationItems.Length == 0)
            {
                notificationMenu.gameObject.SetActive(false);
                return;
            }
            else notificationMenu.gameObject.SetActive(true);
            // hide unused items
            for (int i = notificationItems.Length; i < notificationMenu.childCount; i++)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    Destroy(notificationMenu.GetChild(i).gameObject);
                else
                {
                    var menuItem = notificationMenu.GetChild(i);
                    menuItem.gameObject.SetActive(false);
                    menuItem.name = $"notification-item-{i}-hidden";
                }
#else
                    Destroy(notificationMenu.GetChild(i).gameObject);
#endif
            }
            // update items
            for (int i = 0; i < notificationItems.Length; i++)
            {
                var item = notificationItems[i];
                var menuItem = notificationMenu.childCount > i ? notificationMenu.GetChild(i) : Instantiate(notificationItemPrefab, notificationMenu).transform;
                menuItem.name = $"notification-item-{i}-{item.key}";
                var ri = menuItem.GetComponentInChildren<RawImage>();
                var tl = menuItem.GetComponentInChildren<TextLanguage>();
                if (ri != null)
                {
                    if (item.icon != null)
                        ri.texture = item.icon;
                    ri.gameObject.SetActive(item.icon != null);
                }
                if (tl != null)
                {
                    if (!string.IsNullOrEmpty(item.text))
                        tl.UpdateText(item.text);
                    tl.gameObject.SetActive(!string.IsNullOrEmpty(item.text));
                }
                menuItem.GetComponent<Button>().interactable = item.flags.HasFlag(MenuItemActiveFlags.Interactable);
                menuItem.gameObject.SetActive(item.flags.HasFlag(MenuItemActiveFlags.Enabled));
                UpdateSize(menuItem).Forget();
            }
            UpdateSize(null).Forget();
        }

        private async UniTask UpdateSize(Transform menuItem)
        {
            if (menuItem == null)
            {
                await UniTask.DelayFrame(10);
                notificationMenu.gameObject.SetActive(false);
                await UniTask.DelayFrame(1);
                notificationMenu.gameObject.SetActive(true);
                return;
            }
            var refi = Reference.GetReference("nav.sizeref", menuItem.gameObject);
            if (refi != null)
            {
                await UniTask.DelayFrame(10);
                var rec = menuItem.GetComponent<RectTransform>();
                var reci = refi.GetComponent<RectTransform>();
                var csf = refi.GetComponent<HorizontalOrVerticalLayoutGroup>();
                rec.sizeDelta = new Vector2(csf.preferredWidth, csf.preferredHeight);
            }
        }

        private void UpdatePosition(RectTransform menu, Vector2 direction)
        {
            if (menu == null) return;
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

        private void UpdateContent(RectTransform menu, MenuItem[] items)
        {
            if (menu == null || items == null) return;

            if (items.Length == 0)
            {
                menu.gameObject.SetActive(false);
                return;
            }
            else menu.gameObject.SetActive(true);

            // hide unused items
            for (int i = items.Length; i < menu.childCount; i++)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    Destroy(menu.GetChild(i).gameObject);
                else
                {
                    var menuItem = menu.GetChild(i);
                    menuItem.gameObject.SetActive(false);
                    menuItem.name = $"menu-item-{i}-hidden";
                }
#else
                    Destroy(menu.GetChild(i).gameObject);
#endif
            }
            // update items
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var menuItem = menu.childCount > i ? menu.GetChild(i) : Instantiate(menuItemPrefab, menu).transform;
                menuItem.name = $"menu-item-{i}-{item.key}";
                menuItem.GetComponentInChildren<RawImage>().texture = item.icon;
                menuItem.GetComponentInChildren<TextLanguage>().UpdateText(item.text);
                menuItem.GetComponent<Button>().interactable = item.flags.HasFlag(MenuItemActiveFlags.Interactable);
                menuItem.gameObject.SetActive(item.flags.HasFlag(MenuItemActiveFlags.Enabled));
            }
        }
    }

    [Flags]
    public enum MenuItemActiveFlags
    {
        None = 0,
        Interactable = 1,
        Enabled = 2
    }


    [Serializable]
    public class MenuItem
    {
        public string key;
        public Texture2D icon;
        public string text;
        public string tooltip;
        public MenuItemActiveFlags flags;
    }
}