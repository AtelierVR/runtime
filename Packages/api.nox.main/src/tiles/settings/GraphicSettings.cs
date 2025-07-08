/*using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;


namespace api.nox.game.settings
{
    public class GraphicSettings : SettingHandler
    {
        internal GraphicSettings()
        {
            id = "game.graphic";
            GetPages = GetInternalPages;
        }
        
        private string FormatLanguage(string text) => text.ToLower().Trim().Replace(" ", "_");

        private SettingPage[] GetInternalPages()
            => new SettingPage[]
            {
                new()
                {
                    id = "",
                    text_key = "setting.graphic.text",
                    title_key = "setting.graphic.title",
                    description_key = "setting.graphic.description",
                    icon = GameClientSystem.CoreAPI.AssetAPI.GetAsset<Texture2D>("icons/graphic.png"),
                    groups = new SettingGroup[]
                    {
                        new()
                        {
                            id = "basic",
                            title_key = "setting.graphic.basic.title",
                            description_key = "setting.graphic.basic.description",
                            entries = new SettingEntry[]
                            {
                                new SelectSettingEntry()
                                {
                                    id = "quality",
                                    title_key = "setting.graphic.quality.title",
                                    description_key = "setting.graphic.quality.description",
                                    value = Quality,
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        Quality = value;
                                        Logger.Log("Quality changed to " + Quality);
                                    },
                                    options_text = QualitySettings.names
                                        .Select(e => LanguageManager.Get("setting.graphic.quality." + FormatLanguage(e)))
                                        .ToArray()
                                },
                                new FPSSettingEntry()
                                {
                                    id = "fps",
                                    value = TargetFrameRate,
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        TargetFrameRate = value;
                                        Logger.Log("FPS changed to " + TargetFrameRate);
                                    }
                                },
                                new SelectSettingEntry()
                                {
                                    id = "anti_aliasing",
                                    title_key = "setting.graphic.anti_aliasing.title",
                                    description_key = "setting.graphic.anti_aliasing.description",
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        AntiAliasing = value;
                                        Logger.Log("Anti-aliasing changed to " + AntiAliasing);
                                    },
                                    value = AntiAliasing,
                                    options_text = new[] { "off", "2x", "4x", "8x" }
                                        .Select(e => LanguageManager.Get("setting.graphic.anti_aliasing." + e))
                                        .ToArray()
                                }
                            }
                        },
                        new()
                        {
                            id = "shadow",
                            title_key = "setting.graphic.shadow.title",
                            description_key = "setting.graphic.shadow.description",
                            entries = new SettingEntry[]
                            {
                                new SelectSettingEntry()
                                {
                                    id = "quality",
                                    title_key = "setting.graphic.shadow_quality.title",
                                    description_key = "setting.graphic.shadow_quality.description",
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        ShadowQuality = (ShadowQuality)value;
                                        Logger.Log("Shadow quality changed to " + ShadowQuality);
                                    },
                                    value = (int)ShadowQuality,
                                    options_text = Enum.GetNames(typeof(ShadowQuality))
                                        .Select(e => LanguageManager.Get("setting.graphic.shadow_quality." + FormatLanguage(e)))
                                        .ToArray()
                                },
                                new RangeSettingEntry()
                                {
                                    id = "distance",
                                    title_key = "setting.graphic.shadow_distance.title",
                                    description_key = "setting.graphic.shadow_distance.description",
                                    value = ShadowDistance,
                                    value_key = "setting.range.value.distance.value",
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        ShadowDistance = value;
                                        Logger.Log("Shadow distance changed to " + ShadowDistance);
                                    },
                                    min = 0f,
                                    max = 200f
                                },
                                new SelectSettingEntry()
                                {
                                    id = "projection",
                                    title_key = "setting.graphic.shadow_projection.title",
                                    description_key = "setting.graphic.shadow_projection.description",
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        ShadowProjection = (ShadowProjection)value;
                                        Logger.Log("Shadow projection changed to " + ShadowProjection);
                                    },
                                    value = (int)ShadowProjection,
                                    options_text = Enum.GetNames(typeof(ShadowProjection))
                                        .Select(e => LanguageManager.Get("setting.graphic.shadow_projection." + FormatLanguage(e)))
                                        .ToArray()
                                },
                                new RangeSettingEntry()
                                {
                                    id = "cascades",
                                    title_key = "setting.graphic.shadow_cascades.title",
                                    description_key = "setting.graphic.shadow_cascades.description",
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        ShadowCascades = (int)value;
                                        Logger.Log("Shadow cascades changed to " + ShadowCascades);
                                    },
                                    value = ShadowCascades,
                                    min = 0f,
                                    max = 4f,
                                    step = 1f
                                }
                            }
                        },
                        new()
                        {
                            id = "advanced",
                            title_key = "setting.graphic.advanced.title",
                            description_key = "setting.graphic.advanced.description",
                            entries = new SettingEntry[]
                            {
                                new RangeSettingEntry()
                                {
                                    id = "lodbias",
                                    title_key = "setting.graphic.lodbias.title",
                                    description_key = "setting.graphic.lodbias.description",
                                    value = LodBias,
                                    value_key = "setting.range.value.percent.float",
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        LodBias = value;
                                        Logger.Log("LOD bias changed to " + LodBias);
                                    },
                                    min = 0.1f,
                                    max = 2f
                                },
                                new RangeSettingEntry()
                                {
                                    id = "particule_raycast_budget",
                                    title_key = "setting.graphic.particule_raycast_budget.title",
                                    description_key = "setting.graphic.particule_raycast_budget.description",
                                    value = ParticleRaycastBudget,
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        ParticleRaycastBudget = (int)value;
                                        Logger.Log("Particle raycast budget changed to " + ParticleRaycastBudget);
                                    },
                                    min = 0f,
                                    max = 4096f,
                                    step = 32f
                                },
                                new RangeSettingEntry()
                                {
                                    id = "pixel_light_count",
                                    title_key = "setting.graphic.pixel_light_count.title",
                                    description_key = "setting.graphic.pixel_light_count.description",
                                    value = PixelLightCount,
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        PixelLightCount = (int)value;
                                        Logger.Log("Pixel light count changed to " + PixelLightCount);
                                    },
                                    min = 0f,
                                    max = 8f,
                                    step = 1f
                                },
                                new SelectSettingEntry()
                                {
                                    id = "anisotropic_filtering",
                                    title_key = "setting.graphic.anisotropic_filtering.title",
                                    description_key = "setting.graphic.anisotropic_filtering.description",
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        AnisotropicFiltering = (AnisotropicFiltering)value;
                                        Logger.Log("Anisotropic filtering changed to " + AnisotropicFiltering);
                                    },
                                    value = (int)AnisotropicFiltering,
                                    options_text = Enum.GetNames(typeof(AnisotropicFiltering))
                                        .Select(e => LanguageManager.Get("setting.graphic.anisotropic_filtering." + FormatLanguage(e)))
                                        .ToArray()
                                },
                                new RangeSettingEntry()
                                {
                                    id = "global_texture_mipmap_limit",
                                    title_key = "setting.graphic.global_texture_mipmap_limit.title",
                                    description_key = "setting.graphic.global_texture_mipmap_limit.description",
                                    value = GlobalTextureMipmapLimit,
                                    OnValueChanged = (tile, rect, go, value) =>
                                    {
                                        GlobalTextureMipmapLimit = (int)value;
                                        Logger.Log("Global texture mipmap limit changed to " +
                                                   GlobalTextureMipmapLimit);
                                    },
                                    min = -2f,
                                    max = 4f,
                                    step = 1f
                                }
                            }
                        }
                    }
                }
            };

        int TargetFrameRate
        {
            get
            {
                if (Application.targetFrameRate == -1)
                    return -1;
                if (QualitySettings.vSyncCount == 1)
                    return 0;
                return Application.targetFrameRate;
            }
            set
            {
                if (value == -1)
                {
                    Application.targetFrameRate = -1;
                    QualitySettings.vSyncCount = 0;
                }
                else if (value == 0)
                {
                    Application.targetFrameRate = 0;
                    QualitySettings.vSyncCount = 1;
                }
                else
                {
                    Application.targetFrameRate = value;
                    QualitySettings.vSyncCount = 0;
                }

                SaveToConfig();
            }
        }

        bool IsVSyncFps => TargetFrameRate == 0;
        bool IsCappedFps => TargetFrameRate > 0;
        bool IsUlimitedFps => TargetFrameRate == -1;

        int AntiAliasing
        {
            get => QualitySettings.antiAliasing;
            set
            {
                QualitySettings.antiAliasing = value;
                SaveToConfig();
            }
        }

        int ResolutionWidth
        {
            get => Screen.width;
            set
            {
                Screen.SetResolution(value, ResolutionHeight, IsFullscreen);
                SaveToConfig();
            }
        }

        int ResolutionHeight
        {
            get => Screen.height;
            set
            {
                Screen.SetResolution(ResolutionWidth, value, IsFullscreen);
                SaveToConfig();
            }
        }

        bool IsFullscreen
        {
            get => Screen.fullScreen;
            set
            {
                Screen.fullScreen = value;
                SaveToConfig();
            }
        }

        int Quality
        {
            get => QualitySettings.GetQualityLevel();
            set
            {
                QualitySettings.SetQualityLevel(value);
                SaveToConfig();
            }
        }

        float ShadowDistance
        {
            get => QualitySettings.shadowDistance;
            set
            {
                QualitySettings.shadowDistance = value;
                SaveToConfig();
            }
        }

        ShadowQuality ShadowQuality
        {
            get => QualitySettings.shadows;
            set
            {
                QualitySettings.shadows = value;
                SaveToConfig();
            }
        }

        ShadowProjection ShadowProjection
        {
            get => QualitySettings.shadowProjection;
            set
            {
                QualitySettings.shadowProjection = value;
                SaveToConfig();
            }
        }

        int ShadowCascades
        {
            get => QualitySettings.shadowCascades;
            set
            {
                QualitySettings.shadowCascades = value;
                SaveToConfig();
            }
        }


        int ParticleRaycastBudget
        {
            get => QualitySettings.particleRaycastBudget;
            set
            {
                QualitySettings.particleRaycastBudget = value;
                SaveToConfig();
            }
        }

        int PixelLightCount
        {
            get => QualitySettings.pixelLightCount;
            set
            {
                QualitySettings.pixelLightCount = value;
                SaveToConfig();
            }
        }

        AnisotropicFiltering AnisotropicFiltering
        {
            get => QualitySettings.anisotropicFiltering;
            set
            {
                QualitySettings.anisotropicFiltering = value;
                SaveToConfig();
            }
        }

        int GlobalTextureMipmapLimit
        {
            get => QualitySettings.globalTextureMipmapLimit;
            set
            {
                QualitySettings.globalTextureMipmapLimit = value;
                SaveToConfig();
            }
        }

        // float FieldOfView
        // {
        //     get => Camera.main.fieldOfView;
        //     set => Camera.main.fieldOfView = value;
        // }

        // float NearDistance
        // {
        //     get => Camera.main.nearClipPlane;
        //     set => Camera.main.nearClipPlane = value;
        // }

        float LodBias
        {
            get => QualitySettings.lodBias;
            set
            {
                QualitySettings.lodBias = value;
                SaveToConfig();
            }
        }

        public void LoadFromConfig()
        {
            var config = Config.Load();

            // FPS
            TargetFrameRate = config.Get("settings.graphic.fps", TargetFrameRate);

            // Anti-aliasing
            AntiAliasing = config.Get("settings.graphic.anti_aliasing", AntiAliasing);

            // Resolution
            ResolutionWidth = config.Get("settings.graphic.resolution.width", ResolutionWidth);
            ResolutionHeight = config.Get("settings.graphic.resolution.height", ResolutionHeight);
            IsFullscreen = config.Get("settings.graphic.fullscreen", IsFullscreen);

            // Quality
            Quality = config.Get("settings.graphic.quality", Quality);

            // Particle raycast budget
            ParticleRaycastBudget = config.Get("settings.graphic.particle.raycast_budget", ParticleRaycastBudget);

            // Pixel light count
            PixelLightCount = config.Get("settings.graphic.pixel_light_count", PixelLightCount);

            // Shadow distance
            ShadowDistance = config.Get("settings.graphic.shadow.distance", ShadowDistance);

            // Shadow quality
            ShadowQuality = (ShadowQuality)config.Get("settings.graphic.shadow.quality", (uint)ShadowQuality);

            // Shadow projection
            ShadowProjection =
                (ShadowProjection)config.Get("settings.graphic.shadow.projection", (uint)ShadowProjection);

            // Shadow cascades
            ShadowCascades = config.Get("settings.graphic.shadow.cascades", ShadowCascades);

            // Anisotropic filtering
            AnisotropicFiltering = config.Get("settings.graphic.anisotropic_filtering", AnisotropicFiltering);

            // Global texture mipmap limit
            GlobalTextureMipmapLimit = config.Get("settings.graphic.gtml", GlobalTextureMipmapLimit);

            // Camera
            // FieldOfView = config.Get("settings.graphic.fov", FieldOfView);
            // NearDistance = config.Get("settings.graphic.near_distance", NearDistance);

            // Level of detail
            LodBias = config.Get("settings.graphic.lodbias", LodBias);
        }

        public void SaveToConfig()
        {
            var config = Config.Load();

            // FPS
            config.Set("settings.graphic.fps", TargetFrameRate);

            // Anti-aliasing
            config.Set("settings.graphic.anti_aliasing", AntiAliasing);

            // Resolution
            config.Set("settings.graphic.resolution.width", ResolutionWidth);
            config.Set("settings.graphic.resolution.height", ResolutionHeight);
            config.Set("settings.graphic.fullscreen", IsFullscreen);

            // Quality
            config.Set("settings.graphic.quality", Quality);

            // Shadow
            config.Set("settings.graphic.shadow.distance", ShadowDistance);
            config.Set("settings.graphic.shadow.quality", (uint)ShadowQuality);
            config.Set("settings.graphic.shadow.projection", (uint)ShadowProjection);
            config.Set("settings.graphic.shadow.cascades", ShadowCascades);

            // Particle
            config.Set("settings.graphic.particle.raycast_budget", ParticleRaycastBudget);

            // Light
            config.Set("settings.graphic.light.pixel_count", PixelLightCount);

            // Anisotropic filtering
            config.Set("settings.graphic.anisotropic_filtering", AnisotropicFiltering);

            // Global texture mipmap limit
            config.Set("settings.graphic.gtml", GlobalTextureMipmapLimit);

            //Camera
            // config.Set("settings.graphic.fov", FieldOfView);
            // config.Set("settings.graphic.near_distance", NearDistance);

            // Level of detail
            config.Set("settings.graphic.lodbias", LodBias);

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