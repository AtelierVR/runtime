using api.nox.network.RelayInstances.Base;
using api.nox.network.Utils;

namespace api.nox.network.RelayInstances.Config {
    public class SendConfigReady : InstanceRequest {
        public override Buffer ToBuffer() {
            var buffer = new Buffer();
            buffer.Write(ConfigurationAction.Ready);
            return buffer;
        }
    }
}