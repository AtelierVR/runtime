using System;
using WS = System.Net.WebSockets.ClientWebSocket;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.network
{
    public class WebSocket : ShareObject, IDisposable
    {
        private readonly NetworkSystem _mod;
        public string address;
        public string url;
        public WS driver;

        public event Action<string> OnMessage;
        public event Action OnClose;

        public WebSocket(string address, string ws_url, NetworkSystem mod)
        {
            _mod = mod;
            this.address = address;
            url = ws_url;
            _mod._ws.SetWebSocket(this);
        }

        public async UniTask<bool> Connect(string url = null, WS initial = null)
        {
            if (url != null) this.url = url;
            if (string.IsNullOrEmpty(this.url)) return false;
            Debug.Log($"Connecting to {this.url}");
            driver = initial ?? new WS();
            if (driver.State != System.Net.WebSockets.WebSocketState.Open)
                try
                {
                    await driver.ConnectAsync(new Uri(this.url), default);
                }
                catch { return false; }
            if (driver.State == System.Net.WebSockets.WebSocketState.Open)
            {
                Receive().Forget();
                return true;
            }
            await Close();
            return false;
        }

        public async UniTask Send(string message)
        {
            if (driver == null) return;
            await driver.SendAsync(System.Text.Encoding.UTF8.GetBytes(message), System.Net.WebSockets.WebSocketMessageType.Text, true, default);
        }

        private bool isRunning = false;

        public async UniTaskVoid Receive()
        {
            if (driver == null) return;
            if (isRunning) return;
            isRunning = true;
            while (driver.State == System.Net.WebSockets.WebSocketState.Open && isRunning)
            {
                var buffer = new byte[1024];
                var result = await driver.ReceiveAsync(new ArraySegment<byte>(buffer), default);
                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close) break;
                var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                OnMessage?.Invoke(message);
            }
            OnClose?.Invoke();
            isRunning = false;
        }

        public async UniTask Close()
        {
            isRunning = false;
            try { await driver?.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Closed", default); }
            catch { }
            driver?.Dispose();
            driver = null;
        }



        public void Dispose()
        {
            Close().Forget();
            _mod._ws.RemoveWebSocket(this);
        }

        [ShareObjectExport] public Func<string> SharedGetAddress;
        [ShareObjectExport] public Func<string> SharedGetUrl;
        [ShareObjectExport] public Func<WS> SharedGetDriver;

        [ShareObjectExport] public Action<Action<string>> SharedOnMessage;
        [ShareObjectExport] public Action<Action<string>> SharedOffMessage;
        [ShareObjectExport] public Action<Action> SharedOnClose;
        [ShareObjectExport] public Action<Action> SharedOffClose;

        [ShareObjectExport] public Func<UniTask> SharedClose;
        [ShareObjectExport] public Func<string, UniTask> SharedEmitString;

        public void BeforeExport()
        {
            SharedGetAddress = () => address;
            SharedGetUrl = () => url;
            SharedGetDriver = () => driver;
            SharedOnMessage = action => OnMessage += action;
            SharedOnClose = action => OnClose += action;
            SharedOffMessage = action => OnMessage -= action;
            SharedOffClose = action => OnClose -= action;
            SharedClose = async () => await Close();
            SharedEmitString = async (msg) => await Send(msg);
        }
    }
}