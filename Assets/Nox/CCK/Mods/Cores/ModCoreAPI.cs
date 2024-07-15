using System.Collections.Generic;
using Nox.CCK.Mods.Chat;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Groups;
using Nox.CCK.Mods.Mods;
using Nox.CCK.Mods.Assets;
using Nox.CCK.Mods.XR;

namespace Nox.CCK.Mods.Cores
{
    public interface ModCoreAPI
    {
        public Dictionary<string, object> Data { get; }
        public ModMetadata ModMetadata { get; }
        public ChatAPI ChatAPI { get; }
        public GroupAPI GroupAPI { get; }
        public EventAPI EventAPI { get; }
        public ModAPI ModAPI { get; }
        public AssetAPI AssetAPI { get; }
        public XRAPI XRAPI { get; }
    }
}