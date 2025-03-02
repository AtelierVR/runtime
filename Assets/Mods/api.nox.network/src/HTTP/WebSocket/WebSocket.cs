using System;
using Cysharp.Threading.Tasks;
using System.Net.WebSockets;
using Nox.CCK.Utils;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.network.WebSockets
{
    public class WebSocket : IDisposable, INoxObject
    {
        public readonly string Address;
        private Uri _wsAddress;
        private ClientWebSocket _driver;
        private bool _isRunning;

        [NoxPublic(NoxAccess.Read)] public UnityEvent<string> OnMessage = new();
        [NoxPublic(NoxAccess.Read)] public UnityEvent OnClose = new();

        public WebSocket(string address, string wsURL) : this(address, new Uri(wsURL))
        {
        }

        public WebSocket(string address, Uri wsURL)
        {
            if (NetworkSystem.ModInstance == null) throw new Exception("NetworkSystem is not initialized");
            Address = address;
            _wsAddress = wsURL;
            NetworkSystem.ModInstance.WebSocket.SetWebSocket(this);
        }

        public async UniTask<bool> Connect(string url = null, ClientWebSocket initial = null)
        {
            if (url != null) _wsAddress = new Uri(url);
            if (_wsAddress == null) return false;
            Logger.Log($"Connecting [WS] {_wsAddress}...");
            _driver = initial ?? new ClientWebSocket();
            if (_driver.State != WebSocketState.Open)
                try
                {
                    await _driver.ConnectAsync(_wsAddress, default);
                }
                catch
                {
                    return false;
                }

            if (_driver.State == WebSocketState.Open)
            {
                Receive().Forget();
                return true;
            }

            await Close();
            return false;
        }

        public async UniTask Send(string message)
        {
            if (_driver == null) return;
            await _driver.SendAsync(System.Text.Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true,
                default);
        }


        private async UniTaskVoid Receive()
        {
            if (_driver == null) return;
            if (_isRunning) return;
            _isRunning = true;
            while (_driver.State == WebSocketState.Open && _isRunning)
            {
                var buffer = new byte[1024];
                var result = await _driver.ReceiveAsync(new ArraySegment<byte>(buffer), default);
                if (result.MessageType == WebSocketMessageType.Close) break;
                var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                OnMessage?.Invoke(message);
            }

            OnClose?.Invoke();
            _isRunning = false;
        }

        public async UniTask Close()
        {
            _isRunning = false;
            try
            {
                await _driver?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", default);
            }
            catch
            {
            }

            _driver = null;
        }

        public void Dispose()
        {
            Close().Forget();
            NetworkSystem.ModInstance.WebSocket.RemoveWebSocket(this);
        }
    }
}