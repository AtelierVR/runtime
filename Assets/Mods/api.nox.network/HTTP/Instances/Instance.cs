using System;
using api.nox.network.Relays;
using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
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

        public Relay GetRelay() => networkSystem._relays.GetRelay(address);

        [ShareObjectExport] public Func<ShareObject> SharedGetRelay;

        public void BeforeExport()
        {
            SharedGetRelay = () => GetRelay();
        }

        public void AfterExport()
        {
            SharedGetRelay = null;
        }
    }
}