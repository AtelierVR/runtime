using Nox.CCK;
using UnityEngine;


namespace api.nox.game.Settings
{
    public class GraphicSettings : SettingHandler
    {
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

        float LevelOfDetail
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
            LevelOfDetail = config.Get("settings.graphic.lod_bias", LevelOfDetail);

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
            config.Set("settings.graphic.lod_bias", LevelOfDetail);





            config.Save();
        }

    }
}