using Nox.Network.Instances.Base;
using Nox.Scripts;

namespace Nox.Network.Instances.Config {
    public class SendConfigWorldLoaded : InstanceRequest {
        public override Buffer ToBuffer() {
            var buffer = new Buffer();
            buffer.Write(ConfigurationAction.WorldLoaded);
            return buffer;
        }
    }
}