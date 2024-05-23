using Nox.Network.Relays.Base;
using Nox.Scripts;

namespace Nox.Network.Relays.Status
{
    public class RequestStatus : RelayRequest
    {
        public byte Page;

        public override Buffer ToBuffer()
        {
            var buffer = new Buffer();
            if (Page != 0) buffer.Write(Page);
            return buffer;
        }
    }
}