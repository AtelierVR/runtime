using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyInstanceAPI : ShareObject
    {

        [ShareObjectExport, ShareObjectImport] public Func<ShareObject, UniTask<ShareObject>> SharedSearchInstances;
        [ShareObjectExport, ShareObjectImport] public Func<string, uint, UniTask<ShareObject>> SharedGetInstance;
        [ShareObjectExport, ShareObjectImport] public Func<string, uint, ShareObject> SharedGetInstanceInCache;
        [ShareObjectExport, ShareObjectImport] public Func<ShareObject, UniTask<ShareObject>> SharedCreateInstance;
        public async UniTask<SimplyInstanceSearch> SearchInstances(SimplySearchInstanceData data)
            => (await SharedSearchInstances(data))?.Convert<SimplyInstanceSearch>();
        public async UniTask<SimplyInstance> GetInstance(string server, uint instanceId)
            => (await SharedGetInstance(server, instanceId))?.Convert<SimplyInstance>();
        public async UniTask<SimplyInstance> CreateInstance(SimplyCreateInstanceData data)
            => (await SharedCreateInstance(data))?.Convert<SimplyInstance>();
        public SimplyInstance GetInstanceInCache(string server, uint instanceId)
            => SharedGetInstanceInCache(server, instanceId)?.Convert<SimplyInstance>();
    }
}