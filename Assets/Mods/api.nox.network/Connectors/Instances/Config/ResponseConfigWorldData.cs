
using api.nox.network.RelayInstances.Base;
using api.nox.network.Utils;
using Nox.CCK.Mods;

namespace api.nox.network.RelayInstances.Config
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