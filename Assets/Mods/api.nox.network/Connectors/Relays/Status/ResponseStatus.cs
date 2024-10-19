using System.Collections.Generic;
using api.nox.network.RelayInstances;
using api.nox.network.Relays.Base;

namespace api.nox.network.Relays.Status
{
    public class ResponseStatus : RelayResponse
    {
        public RelayFlags Flags;
        public List<RelayInstance> Instances = new();
        public string MasterAddress;
        public byte Page;
        public byte PageCount;

        public override bool FromBuffer(Utils.Buffer buffer)
        {
            Flags = buffer.ReadEnum<RelayFlags>();
            MasterAddress = buffer.ReadString();
            var instanceCount = buffer.ReadByte();
            for (var i = 0; i < instanceCount; i++)
                Instances.Add(new RelayInstance
                {
                    RelayId = RelayId,
                    Flags = buffer.ReadEnum<InstanceFlags>(),
                    InternalId = buffer.ReadUShort(),
                    Id = buffer.ReadUInt(),
                    PlayerCount = buffer.ReadUShort(),
                    MaxPlayerCount = buffer.ReadUShort(),
                });
            Page = buffer.ReadByte();
            PageCount = buffer.ReadByte();
            return true;
        }

        public override string ToString() =>
            $"{GetType().Name}[Flags={Flags}, MasterAddress={MasterAddress}, Instances={Instances.Count}, Page={Page}/{PageCount}]";

    }
}