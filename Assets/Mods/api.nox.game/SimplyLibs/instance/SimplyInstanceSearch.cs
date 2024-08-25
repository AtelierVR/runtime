using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyInstanceSearch : ShareObject
    {
        public SimplyInstance[] instances;
        [ShareObjectImport, ShareObjectExport] public uint total;
        [ShareObjectImport, ShareObjectExport] public uint limit;
        [ShareObjectImport, ShareObjectExport] public uint offset;
        [ShareObjectImport, ShareObjectExport] public ShareObject[] SharedInstances;
        [ShareObjectImport, ShareObjectExport] public Func<bool> SharedHasNext;
        public bool HasNext() => SharedHasNext();
        [ShareObjectImport, ShareObjectExport] public Func<bool> SharedHasPrevious;
        public bool HasPrevious() => SharedHasPrevious();
        [ShareObjectImport, ShareObjectExport] public Func<UniTask<ShareObject>> SharedNext;
        public async UniTask<SimplyInstanceSearch> Next() => (await SharedNext()).Convert<SimplyInstanceSearch>();
        [ShareObjectImport, ShareObjectExport] public Func<UniTask<ShareObject>> SharedPrevious;
        public async UniTask<SimplyInstanceSearch> Previous() => (await SharedPrevious()).Convert<SimplyInstanceSearch>();

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