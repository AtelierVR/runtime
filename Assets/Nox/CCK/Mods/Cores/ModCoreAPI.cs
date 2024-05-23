using Nox.CCK.Mods.Chat;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Groups;
using Nox.CCK.Mods.Networks;
using UnityEngine;

namespace Nox.CCK.Mods.Cores
{
    public interface ModCoreAPI
    {
        public ChatManager ChatAPI { get; }
        public GroupManager GroupAPI { get; }
        public EventManager EventAPI { get; }
        public NetworkManager NetworkAPI { get; }
    }
}