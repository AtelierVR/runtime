using Nox.Network.Instances.Base;
using Nox.Scripts;

namespace Nox.Network.Instances.Quit
{
    public class RequestQuit : InstanceRequest
    {
        public QuitType Type;
        public string Reason;
        
        public override Buffer ToBuffer()
        {
            var buffer = new Buffer();
            buffer.Write((byte)Type);
            if (!string.IsNullOrEmpty(Reason))
                buffer.Write(Reason);
            return buffer;
        }
    }
}