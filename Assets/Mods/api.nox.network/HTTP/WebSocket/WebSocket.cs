using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using UnityEngine;
using System.Net.WebSockets;

namespace api.nox.network.WebSockets
{
    public class WebSocket : IDisposable
    {
        public string Address;
        public Uri WSAddress;
        public ClientWebSocket driver;

        public event Action<string> OnMessage;
        public event Action OnClose;

        public WebSocket(string address, string ws_url) : this(address, new Uri(ws_url)) { }
        public WebSocket(string address, Uri ws_url)
        {
            if (NetworkSystem.ModInstance == null) throw new Exception("NetworkSystem is not initialized");
            Address = address;
            WSAddress = ws_url;
            NetworkSystem.ModInstance.WebSocket.SetWebSocket(this);
        }

        public async UniTask<bool> Connect(string url = null, ClientWebSocket initial = null)
        {
            if (url != null) WSAddress = new Uri(url);
            if (WSAddress == null) return false;
            Debug.Log($"Connecting [WS] {WSAddress}...");
            driver = initial ?? new ClientWebSocket();
            if (driver.State != WebSocketState.Open)
                try
                {
                    await driver.ConnectAsync(WSAddress, default);
                }
                catch { return false; }
            if (driver.State == WebSocketState.Open)
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
            await driver.SendAsync(System.Text.Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, default);
        }

        private bool isRunning = false;

        public async UniTaskVoid Receive()
        {
            if (driver == null) return;
            if (isRunning) return;
            isRunning = true;
            while (driver.State == WebSocketState.Open && isRunning)
            {
                var buffer = new byte[1024];
                var result = await driver.ReceiveAsync(new ArraySegment<byte>(buffer), default);
                if (result.MessageType == WebSocketMessageType.Close) break;
                var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                OnMessage?.Invoke(message);
            }
            OnClose?.Invoke();
            isRunning = false;
        }

        public async UniTask Close()
        {
            isRunning = false;
            try { await driver?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", default); }
            catch { }
            driver = null;
        }

        public void Dispose()
        {
            Close().Forget();
            NetworkSystem.ModInstance.WebSocket.RemoveWebSocket(this);
        }
    }
}