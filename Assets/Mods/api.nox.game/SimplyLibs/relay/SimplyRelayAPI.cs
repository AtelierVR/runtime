using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyRelayAPI : ShareObject
    {
        [ShareObjectImport] public Func<ShareObject, UniTask<ShareObject>> SharedMakeConnection;
        public async UniTask<SimplyMakeRelayConnectionResponse> MakeConnection(SimplyMakeRelayConnectionData data)
            => (await SharedMakeConnection(data))?.Convert<SimplyMakeRelayConnectionResponse>();
        [ShareObjectImport] public Func<string, ShareObject> SharedGetRelay;
        public SimplyRelay GetRelay(string address)
            => SharedGetRelay(address)?.Convert<SimplyRelay>();
        [ShareObjectImport] public Func<ushort, ShareObject> SharedGetRelayById;
        public SimplyRelay GetRelay(ushort id)
            => SharedGetRelayById(id)?.Convert<SimplyRelay>();
        [ShareObjectImport] public Func<string, bool> SharedHasRelay;
        public bool HasRelay(string address)
            => SharedHasRelay(address);
        [ShareObjectImport] public Func<ushort, bool> SharedHasRelayById;
        public bool HasRelay(ushort id)
            => SharedHasRelayById(id);
    }
}