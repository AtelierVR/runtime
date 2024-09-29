using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyWebSocketAPI : ShareObject
    {
        [ShareObjectImport] public Func<string, string, ShareObject> SharedCreateWebSocket;
        public SimplyWebSocket CreateWebSocket(string address, string ws)
            => SharedCreateWebSocket(address, ws)?.Convert<SimplyWebSocket>();
        [ShareObjectImport] public Action<ShareObject> SharedSetWebSocket;
        public void SetWebSocket(SimplyWebSocket socket)
            => SharedSetWebSocket(socket);
        [ShareObjectImport] public Action<ShareObject> SharedRemoveWebSocket;
        public void RemoveWebSocket(SimplyWebSocket socket)
            => SharedRemoveWebSocket(socket);
        [ShareObjectImport] public Func<string, ShareObject> SharedGetWebSocket;
        public SimplyWebSocket GetWebSocket(string address)
            => SharedGetWebSocket(address)?.Convert<SimplyWebSocket>();
    }
}