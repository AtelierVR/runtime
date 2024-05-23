namespace Nox.CCK.Worlds
{
    [System.Serializable]
    public class Asset
    {
        public uint id;
        public uint version;
        public string engine;
        public string platform;
        public bool is_empty;
        public string url;
        public string hash;
        public uint size;

        public bool IsEmpty() => is_empty;
        public string GetUrl() => IsEmpty() ? null : url;
        public string GetHash() => IsEmpty() ? null : hash;
        public uint GetSize() => IsEmpty() ? 0 : size;
        public bool CompatibleEngine() => engine == "unity";
        public bool CompatiblePlatform() => platform == PlatfromExtensions.GetPlatformName(Constants.CurrentPlatform);

        public override string ToString() => $"{GetType().Name}[Id={id}, Version={version}, Engine={engine}, Platform={platform}]";
    }
}