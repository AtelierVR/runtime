using System;
using Nox.Network.Relays.Base;
using Buffer = Nox.Scripts.Buffer;

namespace Nox.Network.Relays.Latency
{
    public class RequestLatency : RelayRequest
    {
        public DateTime InitialTime;
        
        public override Buffer ToBuffer()
        {
            var buffer = new Buffer();
            buffer.Write(InitialTime);
            return buffer;
        }
    }
}