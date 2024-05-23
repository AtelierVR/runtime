#if UNITY_EDITOR
using UnityEditor;

namespace Nox.CCK.Editor
{
    public enum SuppordBuildTarget
    {
        NoTarget = BuildTarget.NoTarget,
        Windows = BuildTarget.StandaloneWindows64,
        Linux = BuildTarget.StandaloneLinux64,
        OSX = BuildTarget.StandaloneOSX,
        Android = BuildTarget.Android
    }

    public class SuppordTarget
    {
        public static string GetTargetName(SuppordBuildTarget target)
        {
            switch (target)
            {
                case SuppordBuildTarget.Windows:
                    return "windows";
                case SuppordBuildTarget.Linux:
                    return "linux";
                case SuppordBuildTarget.OSX:
                    return "macos";
                case SuppordBuildTarget.Android:
                    return "android";
                default:
                    return "unknown";
            }
        }

        public static SuppordBuildTarget GetNameTarget(string target)
        {
            switch (target)
            {
                case "windows":
                    return SuppordBuildTarget.Windows;
                case "linux":
                    return SuppordBuildTarget.Linux;
                case "macos":
                    return SuppordBuildTarget.OSX;
                case "android":
                    return SuppordBuildTarget.Android;
                default:
                    return SuppordBuildTarget.NoTarget;
            }
        }

        public static SuppordBuildTarget GetCurrentTarget() => (SuppordBuildTarget)EditorUserBuildSettings.activeBuildTarget;
    }
}
#endif