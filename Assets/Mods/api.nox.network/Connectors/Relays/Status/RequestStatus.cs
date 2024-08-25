using api.nox.network.Relays.Base;
using api.nox.network.Utils;

namespace api.nox.network.Relays.Status
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