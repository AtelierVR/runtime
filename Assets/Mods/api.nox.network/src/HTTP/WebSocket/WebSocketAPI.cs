using System;
using System.Collections.Generic;

namespace api.nox.network.WebSockets
{
    public class WebSocketAPI : IDisposable
    {
        private List<WebSocket> _sockets = new();
        public void Dispose()
        {
            foreach (var socket in _sockets.ToArray())
                socket.Dispose();
            _sockets.Clear();
            _sockets = null;
        }

        public WebSocket GetWebSocket(string address) => _sockets?.Find(socket => socket.Address == address);
        public WebSocket CreateWebSocket(string address, string ws) => new(address, ws);
        internal void SetWebSocket(WebSocket socket) => _sockets?.Add(socket);
        internal void RemoveWebSocket(WebSocket socket)
        {
            if (socket == null) return;
            if (_sockets.Contains(socket))
                _sockets.Remove(socket);
        }
    }
}