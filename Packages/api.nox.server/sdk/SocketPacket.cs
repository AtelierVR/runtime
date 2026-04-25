using System;

namespace Nox.Servers
{
    [Serializable]
    public class SocketPacket
    {
        public string type;
        public dynamic payload;
        public string id;
    }

    [Serializable]
    public class SocketPacket<T>
    {
        public string type;
        public T payload;
        public string id;
    }
}
