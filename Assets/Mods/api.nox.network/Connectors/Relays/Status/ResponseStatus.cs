using System.Collections.Generic;
using api.nox.network.Instances;
using api.nox.network.Relays.Base;
using api.nox.network.Utils;
using UnityEngine;

namespace api.nox.network.Relays.Status
{
    public class ResponseStatus : RelayResponse
    {
        public RelayFlags Flags;
        public string MasterAddress;
        public List<Instances.Instance> Instances = new();
        public byte Page;
        public byte PageCount;

        public RequestStatus NextPage() => Page + 1 >= PageCount ? null : new RequestStatus { Page = (byte)(Page + 1) };

        public override bool FromBuffer(Buffer buffer)
        {
            Flags = buffer.ReadEnum<RelayFlags>();
            MasterAddress = buffer.ReadString();
            var instanceCount = buffer.ReadByte();
            for (var i = 0; i < instanceCount; i++)
                Instances.Add(new Instances.Instance
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