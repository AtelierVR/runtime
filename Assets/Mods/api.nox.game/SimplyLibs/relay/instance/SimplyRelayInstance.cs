using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyRelayInstance : ShareObject
    {
        [ShareObjectImport] public uint Id;
        [ShareObjectImport] public ushort InternalId;
        [ShareObjectImport] public string ServerAdress;
        [ShareObjectImport] public ushort RelayId;
        [ShareObjectImport] public ushort PlayerCount;
        [ShareObjectImport] public ushort MaxPlayerCount;
        public SimplyRelayInstanceFlags Flags;
        [ShareObjectImport] public uint SharedFlags;
        [ShareObjectImport] public Func<ShareObject> SharedGetRelay;
        public SimplyRelay Relay => SharedGetRelay()?.Convert<SimplyRelay>();
        [ShareObjectImport] public Func<ShareObject, UniTask<ShareObject>> SharedEnter;
        public async UniTask<SimplyRelayResponseEnter> Enter(SimplyRelayRequestEnter request)
            => (await SharedEnter(request))?.Convert<SimplyRelayResponseEnter>();
        [ShareObjectImport] public Func<UniTask<ShareObject>> SharedQuit;
        public async UniTask<SimplyRelayEventQuit> Quit()
            => (await SharedQuit())?.Convert<SimplyRelayEventQuit>();
        [ShareObjectImport] public Func<bool> SharedSendConfigWorldLoaded;
        public bool SendConfigWorldLoaded() => SharedSendConfigWorldLoaded();
        [ShareObjectImport] public Func<bool> SharedSendConfigReady;
        public bool SendConfigReady() => SharedSendConfigReady();
        [ShareObjectImport] public Func<ShareObject, bool> SharedSendTransform;
        public bool SendTransform(SimplyRelayRequestTransform request)
            => SharedSendTransform(request);

        public void AfterImport()
        {
            Flags = (SimplyRelayInstanceFlags)SharedFlags;
        }
    }
}