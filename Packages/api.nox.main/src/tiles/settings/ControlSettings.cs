/*using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.game.settings
{
    public class ControlSettings : SettingHandler
    {
        internal static ControlSettings Instance;

        internal ControlSettings()
        {
            id = "game.control";
            GetPages = GetInternalPages;
            Instance = this;
        }

        private SettingPage[] GetInternalPages()
            => new SettingPage[]
            {
                new()
                {
                    id = "",
                    text_key = "setting.control.text",
                    title_key = "setting.control.title",
                    description_key = "setting.control.description",
                    icon = GameClientSystem.CoreAPI.AssetAPI.GetAsset<Texture2D>("icons/control.png"),
                    groups = new SettingGroup[]
                    {
                        new()
                        {
                            id = "general",
                            title_key = "setting.control.general.title",
                            description_key = "setting.control.general.description",
                            entries = new SettingEntry[]
                            {
                                new ToggleSettingEntry
                                {
                                    id = "invert_mouse",
                                    title_key = "setting.control.general.invert_mouse",
                                    description_key = "setting.control.general.invert_mouse.description",
                                    value = InvertMouse,
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        InvertMouse = value;
                                        Logger.LogDebug("Invert mouse set to " + value);
                                    }
                                },
                                new RangeSettingEntry()
                                {
                                    id = "mouse_sensitivity",
                                    title_key = "setting.control.general.mouse_sensitivity",
                                    description_key = "setting.control.general.mouse_sensitivity.description",
                                    value = MouseSensitivity,
                                    value_key = "setting.control.general.mouse_sensitivity.value",
                                    min = 0.1f,
                                    max = 10f,
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        MouseSensitivity = value;
                                        Logger.LogDebug("Mouse sensitivity set to " + value);
                                    }
                                }
                            }
                        }
                    }
                }
            };

        public static float AdjustedMouseSensitivity => Instance.MouseSensitivity * (Instance.InvertMouse ? -1 : 1);

        private bool _invertMouse = false;
        private float _mouseSensitivity = 1f;

        public bool InvertMouse
        {
            get => _invertMouse;
            set
            {
                _invertMouse = value;
                SaveToConfig();
            }
        }

        public float MouseSensitivity
        {
            get => _mouseSensitivity;
            set
            {
                _mouseSensitivity = value;
                SaveToConfig();
            }
        }


        public void SaveToConfig()
        {
            var config = Config.Load();
            config.Set("settings.control.mouse_sensitivity", MouseSensitivity);
            config.Set("settings.control.invert_mouse", InvertMouse);
            config.Save();
        }
        
        internal void UpdateHandler()
        {
            GameClientSystem.CoreAPI.EventAPI.Emit("game.setting", this);
        }

        public void OnDispose()
        {
            GetPages = null;
            UpdateHandler();
        }
    }
}*/