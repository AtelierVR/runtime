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

        public void AfterImport()
        {
            Flags = (SimplyRelayInstanceFlags)SharedFlags;
        }

    }
}