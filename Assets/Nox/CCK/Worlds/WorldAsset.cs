// using Nox.CCK.Mods;

// namespace Nox.CCK.Worlds
// {
//     public class Asset : ShareObject
//     {
//         public uint id;
//         public uint version;
//         public string engine;
//         public string platform;
//         public bool is_empty;
//         public string url;
//         public string hash;
//         public uint size;

//         public bool IsEmpty() => is_empty;
//         public string GetUrl() => IsEmpty() ? null : url;
//         public string GetHash() => IsEmpty() ? null : hash;
//         public uint GetSize() => IsEmpty() ? 0 : size;
//         public bool CompatibleEngine() => engine == "unity";
//         public bool CompatiblePlatform() => platform == PlatfromExtensions.GetPlatformName(Constants.CurrentPlatform);
//     }
// }