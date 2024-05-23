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
    }
}