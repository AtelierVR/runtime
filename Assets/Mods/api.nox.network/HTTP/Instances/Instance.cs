using System;
using api.nox.network.Relays;
using Nox.CCK.Mods;

namespace api.nox.network
{
    [Serializable]
    public class Instance : ShareObject
    {
        internal NetworkSystem networkSystem;
        [ShareObjectExport] public uint id;
        [ShareObjectExport] public string title;
        [ShareObjectExport] public string description;
        [ShareObjectExport] public string thumbnail;
        [ShareObjectExport] public string server;
        [ShareObjectExport] public string name;
        [ShareObjectExport] public ushort capacity;
        [ShareObjectExport] public string[] tags;
        [ShareObjectExport] public string world;
        [ShareObjectExport] public string address;
        [ShareObjectExport] public ushort client_count;

        public Relay GetRelay() => networkSystem._relays.GetRelay(address);

        [ShareObjectExport] public Func<ShareObject> SharedGetRelay;
        [ShareObjectExport] public ShareObject[] SharedPlayers;
        public InstancePlayer[] players;


        public void BeforeExport()
        {
            SharedGetRelay = () => GetRelay();
            SharedPlayers = new ShareObject[players.Length];
            for (int i = 0; i < players.Length; i++)
                SharedPlayers[i] = players[i];
        }

        public void AfterExport()
        {
            SharedGetRelay = null;
            SharedPlayers = null;
        }
    }

    [Serializable]
    public class InstancePlayer : ShareObject
    {
        [ShareObjectExport] public string user;
        [ShareObjectExport] public string display;
    }
}