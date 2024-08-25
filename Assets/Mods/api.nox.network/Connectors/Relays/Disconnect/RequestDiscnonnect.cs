using api.nox.network.Relays.Base;
using api.nox.network.Utils;

namespace api.nox.network.Relays.Disconnect
{
    public class RequestDiscnonnect : RelayRequest
    {
        public string Reason;
        
        public override Buffer ToBuffer()
        {
            var buffer = new Buffer();
            if (!string.IsNullOrEmpty(Reason))  
                buffer.Write(Reason);
            return buffer;
        }
        
    }
}