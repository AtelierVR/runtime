using UnityEditor;
using UnityEngine;

namespace Nox.CCK
{
    public enum Platfrom : byte
    {
        None = 0,
        Windows = 1,
        Linux = 2,
        MacOS = 3,
        Android = 4,
        IOS = 5
    }

    public static class PlatfromExtensions
    {
        public static string GetPlatformName(this Platfrom platform) => platform switch
        {
            Platfrom.Windows => "windows",
            Platfrom.Linux => "linux",
            Platfrom.MacOS => "macos",
            Platfrom.Android => "android",
            _ => null,
        };

        public static Platfrom GetPlatformFromName(string name) => name switch
        {
            "windows" => Platfrom.Windows,
            "linux" => Platfrom.Linux,
            "macos" => Platfrom.MacOS,
            "android" => Platfrom.Android,
            _ => Platfrom.None,
        };

        public static Platfrom CurrentPlatform => Application.platform switch
        {
            RuntimePlatform.WindowsPlayer => Platfrom.Windows,
            RuntimePlatform.WindowsEditor => Platfrom.Windows,
            RuntimePlatform.LinuxPlayer => Platfrom.Linux,
            RuntimePlatform.LinuxEditor => Platfrom.Linux,
            RuntimePlatform.LinuxServer => Platfrom.Linux,
            RuntimePlatform.LinuxHeadlessSimulation => Platfrom.Linux,
            RuntimePlatform.OSXPlayer => Platfrom.MacOS,
            RuntimePlatform.OSXEditor => Platfrom.MacOS,
            RuntimePlatform.IPhonePlayer => Platfrom.IOS,
            RuntimePlatform.Android => Platfrom.Android,
            _ => Platfrom.None
        };

#if UNITY_EDITOR
        public static Platfrom GetPlatfromFromBuildTarget(BuildTarget target) => target switch
        {
            BuildTarget.StandaloneWindows => Platfrom.Windows,
            BuildTarget.StandaloneWindows64 => Platfrom.Windows,
            BuildTarget.StandaloneLinux64 => Platfrom.Linux,
            BuildTarget.StandaloneOSX => Platfrom.MacOS,
            BuildTarget.Android => Platfrom.Android,
            _ => Platfrom.None,
        };
#endif
    }
}