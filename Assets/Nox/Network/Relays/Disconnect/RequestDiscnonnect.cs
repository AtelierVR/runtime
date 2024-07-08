using Nox.Network.Relays.Base;
using Nox.Scripts;

namespace Nox.Network.Relays.Disconnect
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