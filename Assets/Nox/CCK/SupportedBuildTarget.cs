#if UNITY_EDITOR
using UnityEditor;

namespace Nox.CCK
{
    public enum SupportBuildTarget
    {
        NoTarget = BuildTarget.NoTarget,
        Windows = BuildTarget.StandaloneWindows64,
        Linux = BuildTarget.StandaloneLinux64,
        OSX = BuildTarget.StandaloneOSX,
        VisionOS = BuildTarget.VisionOS,
        Android = BuildTarget.Android,
        iOS = BuildTarget.iOS,
        WebGL = BuildTarget.WebGL
    }

    public class SuppordTarget
    {
        public static string GetTargetName(SupportBuildTarget target)
        {
            switch (target)
            {
                case SupportBuildTarget.Windows:
                    return "windows";
                case SupportBuildTarget.Linux:
                    return "linux";
                case SupportBuildTarget.OSX:
                    return "macos";
                case SupportBuildTarget.Android:
                    return "android";
                case SupportBuildTarget.iOS:
                    return "ios";
                case SupportBuildTarget.WebGL:
                    return "webgl";
                case SupportBuildTarget.VisionOS:
                    return "visionos";
                case SupportBuildTarget.NoTarget:
                default:
                    return "unknown";
            }
        }

        public static SupportBuildTarget GetNameTarget(string target)
        {
            switch (target)
            {
                case "windows":
                    return SupportBuildTarget.Windows;
                case "linux":
                    return SupportBuildTarget.Linux;
                case "macos":
                    return SupportBuildTarget.OSX;
                case "android":
                    return SupportBuildTarget.Android;
                case "ios":
                    return SupportBuildTarget.iOS;
                case "webgl":
                    return SupportBuildTarget.WebGL;
                case "visionos":
                    return SupportBuildTarget.VisionOS;
                case "unknown":
                default:
                    return SupportBuildTarget.NoTarget;
            }
        }

        public static SupportBuildTarget GetCurrentTarget() => (SupportBuildTarget)EditorUserBuildSettings.activeBuildTarget;
    }
}
#endif