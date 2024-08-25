using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class InstanceSearch : ShareObject
    {
        internal NetworkSystem netSystem;
        public Instance[] instances;
        internal string world;
        internal string owner;
        [ShareObjectExport] public uint total;
        [ShareObjectExport] public uint limit;
        [ShareObjectExport] public uint offset;

        public bool HasNext() => offset + limit < total;
        public bool HasPrevious() => offset > 0;

        public async UniTask<InstanceSearch> Next()
            => HasNext() ? await netSystem.Instance.SearchInstances(new SearchInstanceData()
            {
                world = world,
                owner = owner,
                offset = offset + limit,
                limit = limit
            }) : null;

        public async UniTask<InstanceSearch> Previous()
            => HasPrevious() ? await netSystem.Instance.SearchInstances(new SearchInstanceData()
            {
                world = world,
                owner = owner,
                offset = offset - limit,
                limit = limit
            }) : null;

        [ShareObjectExport] public ShareObject[] SharedInstances;
        [ShareObjectExport] public Func<bool> SharedHasNext;
        [ShareObjectExport] public Func<bool> SharedHasPrevious;
        [ShareObjectExport] public Func<UniTask<ShareObject>> SharedNext;
        [ShareObjectExport] public Func<UniTask<ShareObject>> SharedPrevious;

        public void BeforeExport()
        {
            SharedInstances = new ShareObject[instances.Length];
            for (int i = 0; i < instances.Length; i++)
                SharedInstances[i] = instances[i];
            SharedHasNext = HasNext;
            SharedHasPrevious = HasPrevious;
            SharedNext = async () => await Next();
            SharedPrevious = async () => await Previous();
        }

        public void AfterExport()
        {
            SharedInstances = null;
            SharedHasNext = null;
            SharedHasPrevious = null;
            SharedNext = null;
            SharedPrevious = null;
        }
    }
}