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

            public string SelectedHandler
            {
                get => GetData<string>(0);
                set => SetData(0, value);
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
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.settings");
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
            if (settingHandlers.Count > 0)
                OnSelectHandler(tile, content, settingHandlers.First().Value.id);
            UpdateContent(tile, content);
        }

        private void UpdateContent(SettingTileObject tile, GameObject content)
        {
            var setting_list = Reference.GetReference("setting_list", content).GetComponent<RectTransform>();

            var selectedSetting = tile.GetSettingHandler();
            if (selectedSetting == null && settingHandlers.Count > 0)
                selectedSetting = settingHandlers.First().Value;
            
            foreach (Transform child in setting_list)
                Object.Destroy(child.gameObject);

            var setting_prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.setting.entry");
            setting_prefab.SetActive(false);
            foreach (var setting in settingHandlers)
            {
                var setting_entry = Object.Instantiate(setting_prefab, setting_list);
                setting_entry.name = setting.Key;

                Reference.GetReference("button", setting_entry).GetComponent<Button>()
                    .onClick.AddListener(() => OnSelectHandler(tile, content, setting.Key));

                var no_icon = Reference.GetReference("no_icon", setting_entry);
                var with_icon = Reference.GetReference("with_icon", setting_entry);

                if (setting.Value.icon != null)
                {
                    no_icon.SetActive(false);
                    with_icon.SetActive(true);
                    Reference.GetReference("icon", with_icon).GetComponent<RawImage>().texture = setting.Value.icon;
                    Reference.GetReference("text", with_icon).GetComponent<TextLanguage>().UpdateText(setting.Value.text_key);
                }
                else
                {
                    no_icon.SetActive(true);
                    with_icon.SetActive(false);
                    Reference.GetReference("text", no_icon).GetComponent<TextLanguage>().UpdateText(setting.Value.text_key);
                }

                if (tile.SelectedHandler == setting.Key)
                {
                    /// ...
                }

                setting_entry.SetActive(true);
            }

            var pages = selectedSetting?.GetPages();
            if (pages != null)
            {
                Debug.Log("SettingTileManager.UpdateContent: " + pages.Length);
            }
        }

        private void OnSelectHandler(SettingTileObject tile, GameObject content, string id)
        {


        }

        internal void OnRemove(SettingTileObject tile)
        {
            if (tile.OnSettingUpdated != null)
                OnSettingUpdated.RemoveListener(tile.OnSettingUpdated);
            tile.OnSettingUpdated = null;
        }
    }
}