using System;
using api.nox.network.Relays.Base;
using Buffer = api.nox.network.Utils.Buffer;

namespace api.nox.network.Relays.Latency
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