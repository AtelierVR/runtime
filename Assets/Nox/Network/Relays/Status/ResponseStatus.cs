using System.Collections.Generic;
using Nox.Network.Instances;
using Nox.Network.Relays.Base;
using Nox.Scripts;

namespace Nox.Network.Relays.Status
{
    public class ResponseStatus : RelayResponse
    {
        public RelayFlags Flags;
        public string MasterAddress;
        public List<Instance> Instances = new();
        public byte Page;
        public byte PageCount;

        public RequestStatus NextPage() => Page + 1 >= PageCount ? null : new RequestStatus { Page = (byte)(Page + 1) };

        public override bool FromBuffer(Buffer buffer)
        {
            Flags = buffer.ReadEnum<RelayFlags>();
            MasterAddress = buffer.ReadString();
            var instanceCount = buffer.ReadByte();
            for (var i = 0; i < instanceCount; i++)
                Instances.Add(new Instance
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
            Debug.Log("Page=" + Page + ", PageCount=" + PageCount);
            return true;
        }


        public override string ToString() =>
            $"{GetType().Name}[Flags={Flags}, MasterAddress={MasterAddress}, Instances={Instances.Count}, Page={Page}/{PageCount}]";
    }
}