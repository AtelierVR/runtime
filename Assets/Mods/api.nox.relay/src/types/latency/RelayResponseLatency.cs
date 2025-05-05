using System;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Latency
{
    public class RelayResponseLatency : RelayResponse
    {
        public DateTime InitialTime;
        public DateTime IntermediateTime;
        public DateTime FinalTime;

        public override bool FromBuffer(Buffer buffer)
        {
            if (buffer.length != 16) return false;
            InitialTime = buffer.ReadDateTime();
            IntermediateTime = buffer.ReadDateTime();
            return true;
        }

        public ulong GetUpLatency() => (ulong)(IntermediateTime - InitialTime).TotalMilliseconds;
        public ulong GetDownLatency() => (ulong)(FinalTime - IntermediateTime).TotalMilliseconds;
        public ulong GetLatency() => (ulong)(FinalTime - InitialTime).TotalMilliseconds;

        public override string ToString()
            => $"{GetType().Name}[ping={GetLatency()}ms, up={GetUpLatency()}ms, down={GetDownLatency()}ms]";
    }
}