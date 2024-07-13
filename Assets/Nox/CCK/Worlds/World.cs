using System;
using System.Linq;
using Nox.CCK.Mods;

namespace Nox.CCK.Worlds
{
    [System.Serializable]
    public class World : ShareObject
    {
        public uint id;
        public string title;
        public string description;
        public ushort capacity;
        public string[] tags;
        public string owner;
        public string server;
        public string thumbnail;

        public Asset[] assets;

        public uint GetOwnerId() => uint.Parse(owner.Split(':')[0]);
        public string GetOwnerServer() => owner.Split(':').Length > 1 ? owner.Split(':')[1] : server;

        public Asset GetAsset(uint id) => assets.FirstOrDefault(a => a.id == id);
        public Asset GetAsset(ushort version)
        {
            if (version != ushort.MaxValue)
                return assets.FirstOrDefault(a => a.version == version && a.CompatibleEngine() && a.CompatiblePlatform());
            return assets.Where(a => a.CompatibleEngine() && a.CompatiblePlatform()).OrderByDescending(a => a.version).FirstOrDefault();
        }

#if UNITY_EDITOR
        public Asset GetAsset(ushort version, SupportBuildTarget target) => assets
            .FirstOrDefault(a => a.version == version && a.CompatibleEngine() && a.platform == SuppordTarget.GetTargetName(target));
#endif

        public override string ToString() => $"{GetType().Name}[Id={id}, Server={server}]";
    }
}