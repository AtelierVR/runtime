using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.game.settings;
using api.nox.game.Tiles;
using api.nox.game.UI;
using Nox.CCK.Mods.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;
using Nox.CCK.Utils;
using Nox.CCK.Language;
using AudioSettings = api.nox.game.settings.AudioSettings;
using Transform = UnityEngine.Transform;

namespace api.nox.game.tiles
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

        [Serializable]
        public class SettingUpdatedEvent : UnityEvent<SettingHandler>
        {
        }

        public SettingUpdatedEvent OnSettingUpdated;

        private GraphicSettings graphic;
        private AudioSettings audio;
        private ControlSettings control;
        private AccessibilitySettings accessibility;
        private PerformanceSettings performance;

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

            control = new ControlSettings();
            control.SaveToConfig();
            // settingHandlers.Add("api.nox.game.settings.control", control);

            accessibility = new AccessibilitySettings();
            accessibility.LoadFromConfig();
            accessibility.SaveToConfig();
            // settingHandlers.Add("api.nox.game.settings.accessibility", accessibility);
            
            performance = new PerformanceSettings();
            performance.LoadFromConfig();
            performance.SaveToConfig();
            

            settingHandlers = new Dictionary<string, SettingHandler>();
        }

        internal void PostInitialize()
        {
            Logger.Log("SettingTileManager.PostInitialize");
            graphic.UpdateHandler();
            audio.UpdateHandler();
            control.UpdateHandler();
            accessibility.UpdateHandler();
            performance.UpdateHandler();
        }

        public void OnDispose()
        {
            graphic.OnDispose();
            audio.OnDispose();
            control.OnDispose();
            accessibility.OnDispose();
            performance.OnDispose();

            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_sub);
            _sub = null;
            OnSettingUpdated.RemoveAllListeners();
            OnSettingUpdated = null;
            foreach (var handler in settingHandlers)
                handler.Value.Dispose();
            settingHandlers.Clear();
            settingHandlers = null;
            Instance = null;
        }

        private void OnSettingHandler(EventData context)
        {
            if (context.Data[0] is not SettingHandler handler) return;
            if (settingHandlers.ContainsKey(handler.id) && handler.GetPages == null)
            {
                settingHandlers.Remove(handler.id);
                // // if (tile != null) UpdateContent(tile);
                // if (selectedHandler == handler.id)
                //     OnSelectHandler(null, null, null);
                return;
            }

            if (handler.GetPages == null) return;
            settingHandlers[handler.id] = handler;
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
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/setting/content.prefab");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = tile.id;

            if (tile.OnSettingUpdated != null)
                OnSettingUpdated.RemoveListener(tile.OnSettingUpdated);
            tile.OnSettingUpdated = (setting) => UpdateContent(tile, content);
            OnSettingUpdated.AddListener(tile.OnSettingUpdated);

            if (!string.IsNullOrEmpty(tile.SelectedHandler))
                return content;

            var first = settingHandlers.FirstOrDefault().Value;
            if (first == null)
                return content;

            tile.SelectedHandler = first.id;
            var firstPage = first.GetPages()?.FirstOrDefault();
            if (firstPage != null)
                tile.SelectedPage = firstPage.id;

            return content;
        }

        internal void OnDisplay(SettingTileObject tile, GameObject content)
        {
            UpdateAllContent(tile, content, true);
        }

        private void UpdateAllContent(SettingTileObject tile, GameObject content, bool goTop = false)
        {
            UpdateList(tile, content);
            UpdateContent(tile, content, goTop);
            ForceUpdateLayout.UpdateManually(content);
        }

        private void OnSelectHandler(SettingTileObject tile, GameObject content, string id, string page = null,
            string value = null, bool goTop = false)
        {
            if (tile.SelectedHandler == id && tile.SelectedPage == page && tile.SelectedValue == value)
                return;

            var currentHandler = tile.GetSettingHandler();
            currentHandler?.OnDeselected?.Invoke(tile, content);
            var currentPage = currentHandler?.GetPages()?.FirstOrDefault(p => p.id == tile.SelectedPage);
            currentPage?.OnDeselected?.Invoke(tile, content);

            tile.SelectedHandler = id;
            tile.SelectedPage = page;
            tile.SelectedValue = value;

            var newHandler = tile.GetSettingHandler();
            newHandler?.OnSelected?.Invoke(tile, content);
            var newPage = newHandler?.GetPages()?.FirstOrDefault(p => p.id == tile.SelectedPage);
            newPage?.OnSelected?.Invoke(tile, content);

            UpdateContent(tile, content, goTop);
        }

        private void UpdateList(SettingTileObject tile, GameObject content)
        {
            var settingList = Reference.GetComponent<RectTransform>("setting_list", content);
            foreach (Transform child in settingList)
                Object.Destroy(child.gameObject);

            var settingPrefab = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/setting/list.prefab");
            settingPrefab.SetActive(false);
            foreach (var setting in settingHandlers)
                if (setting.Value.GetPages != null)
                    foreach (var page in setting.Value.GetPages())
                    {
                        if (page == null) continue;
                        var settingEntry = Object.Instantiate(settingPrefab, settingList);
                        settingEntry.name = setting.Key + (string.IsNullOrEmpty(page.id) ? "" : "." + page.id);

                        var btn = Reference.GetComponent<Button>("button", settingEntry);

                        btn.onClick.AddListener(() => OnSelectHandler(tile, content, setting.Key, page.id, null, true));

                        var noIcon = Reference.GetReference("no_icon", settingEntry);
                        var withIcon = Reference.GetReference("with_icon", settingEntry);

                        if (page.icon)
                        {
                            noIcon.SetActive(false);
                            withIcon.SetActive(true);
                            Reference.GetComponent<RawImage>("icon", withIcon).texture = page.icon;
                            Reference.GetComponent<TextLanguage>("text", withIcon)
                                .UpdateText(page.text_key);
                        }
                        else
                        {
                            noIcon.SetActive(true);
                            withIcon.SetActive(false);
                            Reference.GetComponent<TextLanguage>("text", noIcon)
                                .UpdateText(page.text_key);
                        }

                        settingEntry.SetActive(true);
                    }

            ForceUpdateLayout.UpdateManually(settingList);
        }

        private void UpdateContent(SettingTileObject tile, GameObject content, bool goTop = false)
        {
            var setting = tile.GetSettingHandler();
            var page = setting?.GetPages()?.FirstOrDefault(p => p.id == tile.SelectedPage);
            var id = "" + setting?.id + (!string.IsNullOrEmpty(page?.id) ? page.id : null);
            Reference.GetComponent<TextLanguage>("setting_title", content)
                .UpdateText(page?.title_key ?? id);
            Reference.GetComponent<RawImage>("setting_icon", content).texture = page?.icon;

            var settingContent = Reference.GetComponent<RectTransform>("setting_content", content);
            foreach (Transform child in settingContent)
                Object.Destroy(child.gameObject);

            if (page == null)
            {
                Logger.Log("HUM");
            }
            else
            {
                var settingGroupPrefab = GameClientSystem.CoreAPI.AssetAPI
                    .GetAsset<GameObject>("prefabs/setting/group.prefab");

                settingGroupPrefab.SetActive(false);

                foreach (var group in page.groups)
                {
                    var settingGroup = Object.Instantiate(settingGroupPrefab, settingContent);
                    settingGroup.name = setting.id
                                        + (string.IsNullOrEmpty(page.id) ? "" : "." + page.id)
                                        + (string.IsNullOrEmpty(group.id) ? "" : "." + group.id);

                    Reference.GetComponent<TextLanguage>("title", settingGroup)
                        .UpdateText(group.title_key ?? page.title_key ?? settingGroup.name);

                    var settingGroupContent =
                        Reference.GetComponent<RectTransform>("content", settingGroup);
                    foreach (Transform child in settingGroupContent)
                        Object.Destroy(child.gameObject);

                    foreach (var entry in group.entries)
                    {
                        var settingEntry = entry.Make(tile, settingGroupContent);
                        if (!settingEntry) continue;
                        settingEntry.transform.SetParent(settingGroupContent);
                    }

                    settingGroup.SetActive(true);
                }
            }

            ForceUpdateLayout.UpdateManually(settingContent);

            if (goTop)
                Reference.GetComponent<ScrollRect>("setting_view", content).verticalNormalizedPosition = 1;
        }


        private void OnRemove(SettingTileObject tile)
        {
            if (tile.OnSettingUpdated != null)
                OnSettingUpdated.RemoveListener(tile.OnSettingUpdated);
            tile.OnSettingUpdated = null;
        }
    }
}