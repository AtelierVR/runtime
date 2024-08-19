using Nox.CCK.Mods;
using UnityEngine;

namespace Nox.SimplyLibs
{
    public class SimplyWorldSearch : ShareObject
    {
        public SimplyWorld[] worlds;
        [ShareObjectImport, ShareObjectExport] public uint total;
        [ShareObjectImport, ShareObjectExport] public uint limit;
        [ShareObjectImport, ShareObjectExport] public uint offset;

        [ShareObjectImport, ShareObjectExport] public ShareObject[] SharedWorlds;

        public void BeforeImport()
        {
            worlds = null;
        }

        public void AfterImport()
        {
            Debug.Log("SimplyWorldSearch.AfterImport " + SharedWorlds?.Length + "...");
            worlds = new SimplyWorld[SharedWorlds.Length];
            for (int i = 0; i < SharedWorlds.Length; i++)
                worlds[i] = SharedWorlds[i].Convert<SimplyWorld>();
        }

        public void BeforeExport()
        {
            SharedWorlds = new ShareObject[worlds.Length];
            for (int i = 0; i < worlds.Length; i++)
                SharedWorlds[i] = worlds[i];
        }

        public void AfterExport()
        {
            SharedWorlds = null;
        }


    }
}