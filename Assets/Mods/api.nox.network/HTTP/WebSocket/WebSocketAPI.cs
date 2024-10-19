using System;
using System.Collections.Generic;

namespace api.nox.network.WebSockets
{
    public class WebSocketAPI : IDisposable
    {
        internal List<WebSocket> Sockets = new();
        public void Dispose()
        {
            foreach (var socket in Sockets.ToArray())
                socket.Dispose();
            Sockets.Clear();
            Sockets = null;
        }

        public WebSocket GetWebSocket(string address) => Sockets?.Find(socket => socket.Address == address);
        public WebSocket CreateWebSocket(string address, string ws) => new(address, ws);
        internal void SetWebSocket(WebSocket socket) => Sockets?.Add(socket);
        internal void RemoveWebSocket(WebSocket socket)
        {
            if (socket == null) return;
            if (Sockets.Contains(socket))
                Sockets.Remove(socket);
        }
    }
}