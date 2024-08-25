
using api.nox.network.Instances.Base;
using api.nox.network.Utils;

namespace api.nox.network.Instances.Config
{

    public class ResponseConfigWorldData : InstanceResponse
    {
        public uint MasterId;
        public string Address;
        public ushort Version;

        public override bool FromBuffer(Buffer buffer)
        {
            var instanceId = buffer.ReadUShort();
            if (instanceId != InternalId) return false;
            var action = buffer.ReadEnum<ConfigurationAction>();
            if (action != ConfigurationAction.WorldData) return false;
            MasterId = buffer.ReadUInt();
            Address = buffer.ReadString();
            Version = buffer.ReadUShort();
            return true;
        }

        public override string ToString() => $"{GetType().Name}[MasterId={MasterId}, Address={Address}, Version={(Version == ushort.MaxValue ? "latest" : Version.ToString())}]";
    }
}