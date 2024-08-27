
using api.nox.network.Instances.Base;
using api.nox.network.Utils;
using Nox.CCK.Mods;

namespace api.nox.network.Instances.Config
{

    public class ResponseConfigWorldData : InstanceResponse, ShareObject
    {
        [ShareObjectExport] public uint MasterId;
        [ShareObjectExport] public string Address;
        [ShareObjectExport] public ushort Version;

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