using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class InstanceSearch : ShareObject
    {
        public Instance[] instances;
        [ShareObjectExport] public uint total;
        [ShareObjectExport] public uint limit;
        [ShareObjectExport] public uint offset;

        
        [ShareObjectExport] public ShareObject[] SharedInstances;

        public void BeforeExport()
        {
            SharedInstances = new ShareObject[instances.Length];
            for (int i = 0; i < instances.Length; i++)
                SharedInstances[i] = instances[i];
        }

        public void AfterExport()
        {
            SharedInstances = null;
        }
    }
}