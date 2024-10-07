using System;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    [Serializable]
    public class SimplyInstance : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public uint id;
        [ShareObjectImport, ShareObjectExport] public string title;
        [ShareObjectImport, ShareObjectExport] public string description;
        [ShareObjectImport, ShareObjectExport] public string thumbnail;
        [ShareObjectImport, ShareObjectExport] public string server;
        [ShareObjectImport, ShareObjectExport] public string name;
        [ShareObjectImport, ShareObjectExport] public ushort capacity;
        [ShareObjectImport, ShareObjectExport] public string[] tags;
        [ShareObjectImport, ShareObjectExport] public string world;
        [ShareObjectImport, ShareObjectExport] public string address;
        [ShareObjectImport, ShareObjectExport] public ushort client_count;

        public SimplyInstancePlayer[] players;
        [ShareObjectImport, ShareObjectExport] public ShareObject[] SharedPlayers;

        public void AfterImport()
        {
            players = new SimplyInstancePlayer[SharedPlayers.Length];
            for (int i = 0; i < SharedPlayers.Length; i++)
                players[i] = SharedPlayers[i].Convert<SimplyInstancePlayer>();
            SharedPlayers = null;
        }

        public void BeforeExport()
        {
            SharedPlayers = new ShareObject[players.Length];
            for (int i = 0; i < players.Length; i++)
                SharedPlayers[i] = players[i];
        }

        public void AfterExport() => SharedPlayers = null;

        [ShareObjectImport, ShareObjectExport] public Func<ShareObject> SharedGetRelay;
        public SimplyRelay GetRelay() => SharedGetRelay()?.Convert<SimplyRelay>();
    }

    [Serializable]
    public class SimplyInstancePlayer : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string user;
        [ShareObjectImport, ShareObjectExport] public string display;
    }
}