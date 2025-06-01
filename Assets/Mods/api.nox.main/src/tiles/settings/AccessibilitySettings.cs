/*using System.Collections;
using System.Linq;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.game.settings
{
    public class AccessibilitySettings : SettingHandler
    {
        public AccessibilitySettings()
        {
            id = "game.accessibility";
            GetPages = GetInternalPages;
        }

        private SettingPage[] GetInternalPages()
            => new SettingPage[]
            {
                new()
                {
                    id = "",
                    text_key = "setting.accessibility.text",
                    title_key = "setting.accessibility.title",
                    description_key = "setting.accessibility.description",
                    icon = GameClientSystem.CoreAPI.AssetAPI.GetAsset<Texture2D>("icons/accessibility.png"),
                    groups = new SettingGroup[]
                    {
                        new()
                        {
                            id = "interface",
                            title_key = "setting.accessibility.interface.title",
                            description_key = "setting.accessibility.interface.description",
                            entries = new SettingEntry[]
                            {
                                new LanguageSettingEntry
                                {
                                    id = "language",
                                    title_key = "setting.accessibility.interface.language.title",
                                    description_key = "setting.accessibility.interface.language.description",
                                    value = Language,
                                    option_keys = LanguageManager.GetAvailableLanguages(),
                                    option_texts = LanguageManager.GetAvailableLanguages()
                                        .Select(l => LanguageManager.Get(l, "language"))
                                        .ToArray(),
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        Language = value;
                                        Logger.LogDebug("Language changed to " + value);
                                    }
                                }
                            }
                        },
                        new()
                        {
                            id = "visual",
                            title_key = "setting.accessibility.visual.title",
                            description_key = "setting.accessibility.visual.description",
                            entries = new SettingEntry[]
                            {
                                new RangeSettingEntry
                                {
                                    id = "brightness",
                                    title_key = "setting.accessibility.visual.brightness.title",
                                    description_key = "setting.accessibility.visual.brightness.description",
                                    value = Brightness,
                                    value_key = "setting.range.value.percent.float",
                                    min = 0.2f,
                                    max = 1.0f,
                                    step = 0.1f
                                },
                                new RangeSettingEntry
                                {
                                    id = "bloom_intensity",
                                    title_key = "setting.accessibility.visual.bloom_intensity.title",
                                    description_key = "setting.accessibility.visual.bloom_intensity.description",
                                    value = BloomIntensity,
                                    value_key = "setting.range.value.percent.float",
                                    min = 0.0f,
                                    max = 1.0f,
                                    step = 0.1f
                                }
                            }
                        }
                    }
                }
            };

        public float Brightness = 1.0f;
        public float BloomIntensity = 0.0f;

        public string Language
        {
            get => LanguageManager.CurrentLanguage;
            set
            {
                LanguageManager.CurrentLanguage = value;
                SaveToConfig();
            }
        }

        public void LoadFromConfig()
        {
            var config = Config.Load();

            Brightness = config.Get("settings.accessibility.brightness", Brightness);
            BloomIntensity = config.Get("settings.accessibility.bloom_intensity", BloomIntensity);
            Language = config.Get("settings.accessibility.language", Language);
        }

        public void SaveToConfig()
        {
            var config = Config.Load();

            config.Set("settings.accessibility.brightness", Brightness);
            config.Set("settings.accessibility.bloom_intensity", BloomIntensity);
            config.Set("settings.accessibility.language", Language);

            config.Save();
        }


        internal void UpdateHandler()
        {
            Logger.Log("GraphicSettings.UpdateHandler");
            GameClientSystem.CoreAPI.EventAPI.Emit("game.setting", this);
        }

        public void OnDispose()
        {
            GetPages = null;
            UpdateHandler();
        }
    }
}*/