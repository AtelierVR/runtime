using Nox.CCK.Mods.Chats;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Groups;
using Nox.CCK.Mods.Mods;
using Nox.CCK.Mods.Assets;
using Nox.CCK.Mods.Metadata;

namespace Nox.CCK.Mods.Cores
{
    public interface ModCoreAPI
    {
        public ModMetadata ModMetadata { get; }
        public ChatAPI ChatAPI { get; }
        public GroupAPI GroupAPI { get; }
        public EventAPI EventAPI { get; }
        public ModAPI ModAPI { get; }
        public AssetAPI AssetAPI { get; }
    }
}