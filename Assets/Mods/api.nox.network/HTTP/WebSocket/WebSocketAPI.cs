using System;
using System.Collections.Generic;
using Nox.CCK.Mods;

namespace api.nox.network
{
    public class WebSocketAPI : ShareObject, IDisposable
    {
        private readonly NetworkSystem _mod;
        internal WebSocketAPI(NetworkSystem mod) => _mod = mod;
        internal List<WebSocket> _sockets = new();

        public void Dispose()
        {
            foreach (var socket in _sockets.ToArray())
                socket.Dispose();
            _sockets.Clear();
        }

        internal WebSocket GetWebSocket(string address) => _sockets.Find(socket => socket.address == address);

        internal WebSocket CreateWebSocket(string address, string ws)
        {
            var socket = new WebSocket(address, ws, _mod);
            return socket;
        }

        internal void SetWebSocket(WebSocket socket) => _sockets.Add(socket);

        internal void RemoveWebSocket(WebSocket socket)
        {
            if (_sockets.Contains(socket))
                _sockets.Remove(socket);
        }

        [ShareObjectExport] public Func<string, string, ShareObject> SharedCreateWebSocket;
        [ShareObjectExport] public Action<ShareObject> SharedSetWebSocket;
        [ShareObjectExport] public Action<ShareObject> SharedRemoveWebSocket;
        [ShareObjectExport] public Func<string, ShareObject> SharedGetWebSocket;

        public void BeforeExport()
        {
            SharedCreateWebSocket = (address, ws) => SharedCreateWebSocket(address, ws);
            SharedSetWebSocket = socket => SetWebSocket(socket.Convert<WebSocket>());
            SharedRemoveWebSocket = socket => RemoveWebSocket(socket.Convert<WebSocket>());
            SharedGetWebSocket = address => GetWebSocket(address);
        }

        public void AfterExport()
        {
            SharedCreateWebSocket = null;
            SharedSetWebSocket = null;
            SharedRemoveWebSocket = null;
            SharedGetWebSocket = null;
        }
    }
}