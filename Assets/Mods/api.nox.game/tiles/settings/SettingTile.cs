using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.game.Settings;
using api.nox.game.UI;
using Nox.CCK;
using Nox.CCK.Mods.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using AudioSettings = api.nox.game.Settings.AudioSettings;
using Object = UnityEngine.Object;

namespace api.nox.game.Tiles
{
    internal class SettingTileManager : TileManager
    {
        internal static SettingTileManager Instance { get; private set; }

        internal Dictionary<string, SettingHandler> settingHandlers = null;

        internal class SettingTileObject : TileObject
        {
            public UnityAction<SettingHandler> OnSettingUpdated;

            public string[] Selected
            {
                get => GetData<string[]>(0);
                set => SetData(0, value);
            }

            public string SelectedHandler
            {
                get => Selected != null && Selected.Length > 0 ? Selected[0] : null;
                set
                {
                    if (Selected == null || Selected.Length == 0)
                        Selected = new string[] { value };
                    Selected[0] = value;
                }
            }

            public string SelectedPage
            {
                get => Selected != null && Selected.Length > 1 ? Selected[1] : null;
                set
                {
                    if (Selected == null)
                        Selected = new string[] { null, value };
                    else if (Selected.Length < 2)
                    {
                        var temp = new string[2];
                        temp[0] = Selected.Length > 0 ? Selected[0] : null;
                        temp[1] = value;
                        Selected = temp;
                    }
                    Selected[1] = value;
                }
            }

            public string SelectedValue
            {
                get => Selected != null && Selected.Length > 2 ? Selected[2] : null;
                set
                {
                    if (Selected == null)
                        Selected = new string[] { null, null, value };
                    else if (Selected.Length < 3)
                    {
                        var temp = new string[3];
                        temp[0] = Selected.Length > 0 ? Selected[0] : null;
                        temp[1] = Selected.Length > 1 ? Selected[1] : null;
                        temp[2] = value;
                        Selected = temp;
                    }
                    Selected[2] = value;
                }
            }

            public SettingHandler GetSettingHandler()
            {
                if (SelectedHandler == null) return null;
                if (Instance.settingHandlers?.ContainsKey(SelectedHandler) == true)
                    return Instance.settingHandlers[SelectedHandler];
                return null;
            }
        }

        private EventSubscription _sub;

        [Serializable] public class SettingUpdatedEvent : UnityEvent<SettingHandler> { }
        public SettingUpdatedEvent OnSettingUpdated;

        private GraphicSettings graphic;
        private AudioSettings audio;

        internal SettingTileManager()
        {
            Instance = this;
            OnSettingUpdated = new SettingUpdatedEvent();
            _sub = GameClientSystem.CoreAPI.EventAPI.Subscribe("game.setting", OnSettingHandler);

            // Add setting handlers

            graphic = new GraphicSettings();
            graphic.LoadFromConfig();
            graphic.SaveToConfig();
            // settingHandlers.Add("api.nox.game.settings.graphic", graphic);

            audio = new AudioSettings();
            audio.LoadFromConfig();
            audio.SaveToConfig();
            // settingHandlers.Add("api.nox.game.settings.audio", audio);

            settingHandlers = new Dictionary<string, SettingHandler>();
        }

        internal void PostInitialize()
        {
            Debug.Log("SettingTileManager.PostInitialize");
            graphic.UpdateHandler();
            audio.UpdateHandler();
        }

        public void OnDispose()
        {
            graphic.OnDispose();
            audio.OnDispose();

            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_sub);
            _sub = null;
            OnSettingUpdated.RemoveAllListeners();
            OnSettingUpdated = null;
            settingHandlers = null;
            Instance = null;
        }

        private void OnSettingHandler(EventData context)
        {
            Debug.Log("SettingTileManager.OnSettingHandler");
            if (context.Data[0] is not SettingHandler handler) return;
            Debug.Log("SettingTileManager.OnSettingHandler: " + handler.id);
            if (settingHandlers.ContainsKey(handler.id) && handler.GetPages == null)
            {
                settingHandlers.Remove(handler.id);
                // // if (tile != null) UpdateContent(tile);
                // if (selectedHandler == handler.id)
                //     OnSelectHandler(null, null, null);
                return;
            }
            if (handler.GetPages == null) return;
            if (settingHandlers.ContainsKey(handler.id))
                settingHandlers[handler.id] = handler;
            else settingHandlers.Add(handler.id, handler);
        }

        internal void SendTile(EventData context)
        {
            var tile = new SettingTileObject() { id = "api.nox.game.settings", context = context };
            tile.GetContent = (Transform tf) => OnGetContent(tile, tf);
            tile.onDisplay = (str, gameObject) => OnDisplay(tile, gameObject);
            tile.onRemove = () => OnRemove(tile);
            MenuManager.Instance.SendTile(tile.MenuId, tile);
        }

        internal GameObject OnGetContent(SettingTileObject tile, Transform tf)
        {
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/setting/content");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = tile.id;

            if (tile.OnSettingUpdated != null)
                OnSettingUpdated.RemoveListener(tile.OnSettingUpdated);
            tile.OnSettingUpdated = (setting) => UpdateContent(tile, content);
            OnSettingUpdated.AddListener(tile.OnSettingUpdated);

            return content;
        }

        internal void OnDisplay(SettingTileObject tile, GameObject content)
        {
            UpdateAllContent(tile, content);
        }

        private void UpdateAllContent(SettingTileObject tile, GameObject content)
        {
            UpdateList(tile, content);
            UpdateContent(tile, content);
            ForceUpdateLayout.UpdateManually(content);
        }

        private void OnSelectHandler(SettingTileObject tile, GameObject content, string id, string page = null, string value = null)
        {
            var current_handler = tile.GetSettingHandler();
            current_handler?.OnDeselected?.Invoke(tile, content);
            var current_page = current_handler?.GetPages()?.FirstOrDefault(p => p.id == tile.SelectedPage);
            current_page?.OnDeselected?.Invoke(tile, content);

            tile.SelectedHandler = id;
            tile.SelectedPage = page;
            tile.SelectedValue = value;

            var new_handler = tile.GetSettingHandler();
            new_handler?.OnSelected?.Invoke(tile, content);
            var new_page = new_handler?.GetPages()?.FirstOrDefault(p => p.id == tile.SelectedPage);
            new_page?.OnSelected?.Invoke(tile, content);

            UpdateContent(tile, content);
        }

        private void UpdateList(SettingTileObject tile, GameObject content)
        {
            var setting_list = Reference.GetReference("setting_list", content).GetComponent<RectTransform>();
            foreach (Transform child in setting_list)
                Object.Destroy(child.gameObject);

            var setting_prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/setting/list");
            setting_prefab.SetActive(false);
            foreach (var setting in settingHandlers)
                if (setting.Value.GetPages != null)
                    foreach (var page in setting.Value.GetPages())
                    {
                        if (page == null) continue;
                        var setting_entry = Object.Instantiate(setting_prefab, setting_list);
                        setting_entry.name = setting.Key + (string.IsNullOrEmpty(page.id) ? "" : "." + page.id);

                        Reference.GetReference("button", setting_entry).GetComponent<Button>()
                            .onClick.AddListener(() => OnSelectHandler(tile, content, setting.Key, page.id));

                        var no_icon = Reference.GetReference("no_icon", setting_entry);
                        var with_icon = Reference.GetReference("with_icon", setting_entry);

                        if (page.icon != null)
                        {
                            no_icon.SetActive(false);
                            with_icon.SetActive(true);
                            Reference.GetReference("icon", with_icon).GetComponent<RawImage>().texture = page.icon;
                            Reference.GetReference("text", with_icon).GetComponent<TextLanguage>().UpdateText(page.text_key);
                        }
                        else
                        {
                            no_icon.SetActive(true);
                            with_icon.SetActive(false);
                            Reference.GetReference("text", no_icon).GetComponent<TextLanguage>().UpdateText(page.text_key);
                        }

                        if (tile.SelectedHandler == setting.Key && tile.SelectedPage == page.id)
                        {
                            /// ...
                        }

                        setting_entry.SetActive(true);
                    }

            ForceUpdateLayout.UpdateManually(setting_list);
        }

        private void UpdateContent(SettingTileObject tile, GameObject content)
        {
            var setting = tile.GetSettingHandler();
            var page = setting?.GetPages()?.FirstOrDefault(p => p.id == tile.SelectedPage);
            var id = "" + setting?.id + (!string.IsNullOrEmpty(page?.id) ? page.id : null);
            Reference.GetReference("setting_title", content).GetComponent<TextLanguage>().UpdateText(page?.title_key ?? id);

            var setting_content = Reference.GetReference("setting_content", content).GetComponent<RectTransform>();
            foreach (Transform child in setting_content)
                Object.Destroy(child.gameObject);

            if (page == null)
            {
                Debug.Log("HUM");
            }
            else
            {
                var setting_group_prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/setting/group");

                setting_group_prefab.SetActive(false);

                foreach (var group in page.groups)
                {
                    if (page == null) continue;
                    var setting_group = Object.Instantiate(setting_group_prefab, setting_content);
                    setting_group.name = setting.id
                        + (string.IsNullOrEmpty(page.id) ? "" : "." + page.id)
                        + (string.IsNullOrEmpty(group.id) ? "" : "." + group.id);

                    Reference.GetReference("title", setting_group).GetComponent<TextLanguage>()
                        .UpdateText(group.title_key ?? page.title_key ?? setting_group.name);

                    var setting_group_content = Reference.GetReference("content", setting_group).GetComponent<RectTransform>();
                    foreach (Transform child in setting_group_content)
                        Object.Destroy(child.gameObject);

                    foreach (var entry in group.entries)
                    {
                        var setting_entry = entry.Make(tile, setting_group_content);
                        if (setting_entry == null) continue;
                        setting_entry.transform.SetParent(setting_group_content);
                    }

                    setting_group.SetActive(true);
                }
            }

            ForceUpdateLayout.UpdateManually(setting_content);
        }


        internal void OnRemove(SettingTileObject tile)
        {
            if (tile.OnSettingUpdated != null)
                OnSettingUpdated.RemoveListener(tile.OnSettingUpdated);
            tile.OnSettingUpdated = null;
        }
    }
}