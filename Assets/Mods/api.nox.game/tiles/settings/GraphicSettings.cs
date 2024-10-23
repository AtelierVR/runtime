using System;
using Nox.CCK;
using UnityEngine;


namespace api.nox.game.Settings
{
    public class GraphicSettings : SettingHandler
    {
        internal GraphicSettings()
        {
            id = "game.graphic";
            GetPages = GetInternalPages;
        }

        private SettingPage[] GetInternalPages()
        {
            return new SettingPage[]
            {
                new() {
                    id = "",
                    text_key = "setting.graphic.text",
                    title_key = "setting.graphic.title",
                    description_key = "setting.graphic.description",
                    groups = new SettingGroup[] {
                        new() {
                            id = "basic",
                            title_key = "setting.graphic.basic.title",
                            description_key = "setting.graphic.basic.description",
                            entries = new SettingEntry[] {
                                new SelectSettingEntry() {
                                    id = "quality" ,
                                    title_key = "setting.graphic.quality.title",
                                    description_key = "setting.graphic.quality.description",
                                    value = Quality,
                                    options = QualityNames
                                },
                                new SelectSettingEntry() {
                                    id = "anti_aliasing",
                                    title_key = "setting.graphic.anti_aliasing.title",
                                    description_key = "setting.graphic.anti_aliasing.description",
                                    value = AntiAliasing,
                                    options = AntiAliasingNames
                                }
                            }
                        },
                        new() {
                            id = "shadow",
                            title_key = "setting.graphic.shadow.title",
                            description_key = "setting.graphic.shadow.description",
                            entries = new SettingEntry[] {
                                new SelectSettingEntry() {
                                    id = "quality",
                                    title_key = "setting.graphic.shadow_quality.title",
                                    description_key = "setting.graphic.shadow_quality.description",
                                    value = (int)ShadowQuality,
                                    options = Enum.GetNames(typeof(ShadowQuality))
                                },
                                new RangeSettingEntry() {
                                    id = "distance",
                                    title_key = "setting.graphic.shadow_distance.title",
                                    description_key = "setting.graphic.shadow_distance.description",
                                    value = ShadowDistance,
                                    value_key = "setting.graphic.shadow.distance.value",
                                    min = 0f,
                                    max = 200f
                                },
                                new SelectSettingEntry() {
                                    id = "projection",
                                    title_key = "setting.graphic.shadow_projection.title",
                                    description_key = "setting.graphic.shadow_projection.description",
                                    value = (int)ShadowProjection,
                                    options = Enum.GetNames(typeof(ShadowProjection))
                                },
                                new RangeSettingEntry() {
                                    id = "cascades",
                                    title_key = "setting.graphic.shadow_cascades.title",
                                    description_key = "setting.graphic.shadow_cascades.description",
                                    value = ShadowCascades,
                                    min = 0f,
                                    max = 4f,
                                    step = 1f
                                }
                            }
                        },
                        new() {
                            id = "advanced",
                            title_key = "setting.graphic.advanced.title",
                            description_key = "setting.graphic.advanced.description",
                            entries = new SettingEntry[] {
                                new RangeSettingEntry() {
                                    id = "lodbias",
                                    title_key = "setting.graphic.lodbias.title",
                                    description_key = "setting.graphic.lodbias.description",
                                    value = LodBias,
                                    value_key = "setting.range.value.percent.float",
                                    min = 0.1f,
                                    max = 2f
                                },
                                new RangeSettingEntry() {
                                    id = "particule_raycast_budget",
                                    title_key = "setting.graphic.particule_raycast_budget.title",
                                    description_key = "setting.graphic.particule_raycast_budget.description",
                                    value = ParticleRaycastBudget,
                                    min = 0f,
                                    max = 4096f,
                                    step = 32f
                                },
                                new RangeSettingEntry() {
                                    id = "pixel_light_count",
                                    title_key = "setting.graphic.pixel_light_count.title",
                                    description_key = "setting.graphic.pixel_light_count.description",
                                    value = PixelLightCount,
                                    min = 0f,
                                    max = 8f,
                                    step = 1f
                                },
                                new SelectSettingEntry() {
                                    id = "anisotropic_filtering",
                                    title_key = "setting.graphic.anisotropic_filtering.title",
                                    description_key = "setting.graphic.anisotropic_filtering.description",
                                    value = (int)AnisotropicFiltering,
                                    options = Enum.GetNames(typeof(AnisotropicFiltering))
                                },
                                new RangeSettingEntry() {
                                    id = "global_texture_mipmap_limit",
                                    title_key = "setting.graphic.global_texture_mipmap_limit.title",
                                    description_key = "setting.graphic.global_texture_mipmap_limit.description",
                                    value = GlobalTextureMipmapLimit,
                                    min = -2f,
                                    max = 4f,
                                    step = 1f
                                }
                            }
                        }
                    }
                }
            };
        }

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
            }
        }

        bool IsVSyncFps => TargetFrameRate == 0;
        bool IsCappedFps => TargetFrameRate > 0;
        bool IsUlimitedFps => TargetFrameRate == -1;

        int AntiAliasing
        {
            get => QualitySettings.antiAliasing;
            set => QualitySettings.antiAliasing = value;
        }

        string[] AntiAliasingNames => new string[] { "Off", "2x", "4x", "8x" };

        int ResolutionWidth
        {
            get => Screen.width;
            set => Screen.SetResolution(value, ResolutionHeight, IsFullscreen);
        }

        int ResolutionHeight
        {
            get => Screen.height;
            set => Screen.SetResolution(ResolutionWidth, value, IsFullscreen);
        }

        bool IsFullscreen
        {
            get => Screen.fullScreen;
            set => Screen.fullScreen = value;
        }

        int Quality
        {
            get => QualitySettings.GetQualityLevel();
            set => QualitySettings.SetQualityLevel(value);
        }

        string[] QualityNames => QualitySettings.names;

        float ShadowDistance
        {
            get => QualitySettings.shadowDistance;
            set => QualitySettings.shadowDistance = value;
        }

        ShadowQuality ShadowQuality
        {
            get => QualitySettings.shadows;
            set => QualitySettings.shadows = value;
        }

        ShadowProjection ShadowProjection
        {
            get => QualitySettings.shadowProjection;
            set => QualitySettings.shadowProjection = value;
        }

        int ShadowCascades
        {
            get => QualitySettings.shadowCascades;
            set => QualitySettings.shadowCascades = value;
        }


        int ParticleRaycastBudget
        {
            get => QualitySettings.particleRaycastBudget;
            set => QualitySettings.particleRaycastBudget = value;
        }

        int PixelLightCount
        {
            get => QualitySettings.pixelLightCount;
            set => QualitySettings.pixelLightCount = value;
        }

        AnisotropicFiltering AnisotropicFiltering
        {
            get => QualitySettings.anisotropicFiltering;
            set => QualitySettings.anisotropicFiltering = value;
        }

        int GlobalTextureMipmapLimit
        {
            get => QualitySettings.globalTextureMipmapLimit;
            set => QualitySettings.globalTextureMipmapLimit = value;
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
            set => QualitySettings.lodBias = value;
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
            ShadowProjection = (ShadowProjection)config.Get("settings.graphic.shadow.projection", (uint)ShadowProjection);

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
            Debug.Log("GraphicSettings.UpdateHandler");
            GameClientSystem.CoreAPI.EventAPI.Emit("game.setting", this);
        }

        public void OnDispose()
        {
            GetPages = null;
            UpdateHandler();
        }

    }
}