/*using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;


namespace api.nox.game.settings
{
    public class PerformanceSettings : SettingHandler
    {
        internal PerformanceSettings()
        {
            id = "game.performance";
            GetPages = GetInternalPages;
        }
        
        private SettingPage[] GetInternalPages()
            => new SettingPage[]
            {
                new()
                {
                    id = "",
                    text_key = "setting.performance.text",
                    title_key = "setting.performance.title",
                    description_key = "setting.performance.description",
                    icon = GameClientSystem.CoreAPI.AssetAPI.GetAsset<Texture2D>("icons/performance.png"),
                    groups = new SettingGroup[]
                    {
                        new()
                        {
                            id = "session",
                            title_key = "setting.performance.session.title",
                            description_key = "setting.performance.session.description",
                            entries = new SettingEntry[]
                            {
                                new RangeSettingEntry
                                {
                                    id = "showing_distance",
                                    title_key = "setting.performance.session.showing_distance",
                                    description_key = "setting.performance.session.showing_distance.description",
                                    min = 0,
                                    max = 1000,
                                    step = 1,
                                    value = (float)ShowingDistance,
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        ShowingDistance = value;
                                        Logger.Log("performanceSettings.ShowingDistance: " + ShowingDistance);
                                    },
                                }
                            }
                        }
                    }
                }
            };

        private static double _showingDistance = 100;
        
        public static double GetShowingDistance() => _showingDistance;

        public double ShowingDistance
        {
            get => _showingDistance;
            set
            {
                _showingDistance = value;
                SaveToConfig();
            }
        }

        public void LoadFromConfig()
        {
            var config = Config.Load();

            // Showing distance
            ShowingDistance = config.Get("settings.performance.session.showing_distance", ShowingDistance);
        }

        public void SaveToConfig()
        {
            var config = Config.Load();

            // Showing distance
            config.Set("settings.performance.session.showing_distance", ShowingDistance);

            config.Save();
        }

        internal void UpdateHandler()
        {
            Logger.Log("performanceSettings.UpdateHandler");
            GameClientSystem.CoreAPI.EventAPI.Emit("game.setting", this);
        }

        public void OnDispose()
        {
            GetPages = null;
            UpdateHandler();
        }
    }
}*/