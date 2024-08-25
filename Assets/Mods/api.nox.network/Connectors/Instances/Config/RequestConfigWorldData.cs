using api.nox.network.Instances.Base;
using api.nox.network.Utils;

namespace api.nox.network.Instances.Config
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