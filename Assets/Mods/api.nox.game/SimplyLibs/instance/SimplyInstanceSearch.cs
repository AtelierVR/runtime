using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyInstanceSearch : ShareObject
    {
        public SimplyInstance[] instances;
        [ShareObjectImport] public uint total;
        [ShareObjectImport] public uint limit;
        [ShareObjectImport] public uint offset;

        
        [ShareObjectImport] public ShareObject[] SharedInstances;

        public void BeforeImport()
        {
            instances = null;
        }

        public void AfterImport()
        {
            instances = new SimplyInstance[SharedInstances.Length];
            for (int i = 0; i < SharedInstances.Length; i++)
                instances[i] = SharedInstances[i].Convert<SimplyInstance>();
        }
    }
}