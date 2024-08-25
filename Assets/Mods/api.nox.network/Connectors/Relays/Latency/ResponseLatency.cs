using System;
using api.nox.network.Relays.Base;
using Buffer = api.nox.network.Utils.Buffer;

namespace api.nox.network.Relays.Latency
{
    public class ResponseLatency : RelayResponse
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
        
        public override string ToString() =>
            $"{GetType().Name}[Latency={GetLatency()}ms, Up={GetUpLatency()}ms, Down={GetDownLatency()}ms]";
    }
}