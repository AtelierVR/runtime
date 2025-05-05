using Nox.CCK.Utils;

namespace api.nox.relay.types
{
    public abstract class Response : INoxObject
    {
        public ushort ConnectionId { get; set; }
        public ushort State;
        public abstract bool FromBuffer(Buffer buffer);
    }
}