using Nox.Network.Instances.Base;
using Nox.Scripts;

namespace Nox.Network.Instances.Config
{
    public class RequestConfigWorldData : InstanceRequest
    {
        public override Buffer ToBuffer()
        {
            var buffer = new Buffer();
            buffer.Write(ConfigurationAction.WorldData);
            return buffer;
        }
    }
}