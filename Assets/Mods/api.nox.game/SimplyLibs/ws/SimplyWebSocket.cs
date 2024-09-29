using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using WS = System.Net.WebSockets.ClientWebSocket;

namespace Nox.SimplyLibs
{
    public class SimplyWebSocket : ShareObject
    {
        [ShareObjectImport] public Func<string> SharedGetAddress;
        [ShareObjectImport] public Func<string> SharedGetUrl;
        [ShareObjectImport] public Func<WS> SharedGetDriver;

        [ShareObjectImport] public Action<Action<string>> SharedOnMessage;
        [ShareObjectImport] public Action<Action<string>> SharedOffMessage;
        [ShareObjectImport] public Action<Action> SharedOnClose;
        [ShareObjectImport] public Action<Action> SharedOffClose;

        [ShareObjectImport] public Func<UniTask> SharedClose;
        [ShareObjectImport] public Func<string, UniTask> SharedEmitString;

        public string address => SharedGetAddress?.Invoke();
        public string url => SharedGetUrl?.Invoke();
        public WS driver => SharedGetDriver?.Invoke();
        public async UniTask Close() => await SharedClose();
        public async UniTask Send(string message) => await SharedEmitString(message);

        public event Action<string> OnMessage
        {
            add => SharedOnMessage?.Invoke(value);
            remove => SharedOffMessage?.Invoke(value);
        }

        public event Action OnClose
        {
            add => SharedOnClose?.Invoke(value);
            remove => SharedOffClose?.Invoke(value);
        }
    }
}