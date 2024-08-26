using System;
using System.Net;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyRelay : ShareObject
    {
        [ShareObjectImport] public Func<ushort> SharedGetId;
        public ushort Id => SharedGetId();
        [ShareObjectImport] public Func<ushort> SharedGetClientId;
        public ushort ClientId => SharedGetClientId();
        [ShareObjectImport] public Func<IPEndPoint> SharedGetEndPoint;
        public IPEndPoint EndPoint => SharedGetEndPoint();
        [ShareObjectImport] public Func<object> SharedGetUserData;
        public object UserData => SharedGetUserData();
        [ShareObjectImport] public Func<byte, UniTask<ShareObject>> SharedRequestStatus;
        public async UniTask<SimplyRelayResponseStatus> RequestStatus(byte page)
            => (await SharedRequestStatus(page))?.Convert<SimplyRelayResponseStatus>();
        [ShareObjectImport] public Func<UniTask<ShareObject>> SharedRequestAllStatus;
        public async UniTask<SimplyRelayResponseStatus> RequestStatus()
            => (await SharedRequestAllStatus())?.Convert<SimplyRelayResponseStatus>();
        [ShareObjectImport] public Func<UniTask<ShareObject>> SharedRequestLatency;
    }
}