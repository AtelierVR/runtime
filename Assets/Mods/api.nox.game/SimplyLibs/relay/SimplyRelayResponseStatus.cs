using System.Collections.Generic;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyRelayResponseStatus : ShareObject
    {
        [ShareObjectImport] public string MasterAddress;
        [ShareObjectImport] public byte Page;
        [ShareObjectImport] public byte PageCount;
        [ShareObjectImport] public byte SharedFlags;
        [ShareObjectImport] public ShareObject[] SharedInstances;

        public SimplyRelayFlags Flags;
        public List<SimplyRelayInstance> Instances = new();

        public void AfterImport()
        {
            Flags = (SimplyRelayFlags)SharedFlags;
            Instances = new List<SimplyRelayInstance>();
            foreach (var instance in SharedInstances)
                Instances.Add(instance.Convert<SimplyRelayInstance>());
        }


    }
}