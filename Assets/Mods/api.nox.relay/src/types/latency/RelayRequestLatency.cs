using System;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Latency
{
    public class RequestLatency : Request
    {
        public static float IntervalLatencyRequest = 2.5f;
        
        public DateTime InitialTime;

        public override Buffer ToBuffer()
        {
            var buffer = new Buffer();
            buffer.Write(InitialTime);
            return buffer;
        }
    }
}