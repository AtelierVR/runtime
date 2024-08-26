using System;
using System.Collections.Generic;
using api.nox.network.Instances;
using api.nox.network.Relays.Base;
using api.nox.network.Utils;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.network.Relays.Status
{
    public class ResponseStatus : RelayResponse, ShareObject
    {
        public RelayFlags Flags;
        public List<Instances.Instance> Instances = new();
        [ShareObjectExport] public string MasterAddress;
        [ShareObjectExport] public byte Page;
        [ShareObjectExport] public byte PageCount;

        public override bool FromBuffer(Utils.Buffer buffer)
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
            return true;
        }

        public override string ToString() =>
            $"{GetType().Name}[Flags={Flags}, MasterAddress={MasterAddress}, Instances={Instances.Count}, Page={Page}/{PageCount}]";

        [ShareObjectExport] public byte SharedFlags;
        [ShareObjectExport] public ShareObject[] SharedInstances;

        public void BeforeExport()
        {
            SharedFlags = (byte)Flags;
            SharedInstances = new ShareObject[Instances.Count];
            for (var i = 0; i < Instances.Count; i++)
                SharedInstances[i] = Instances[i];
        }

        public void AfterExport()
        {
            SharedFlags = 0;
            SharedInstances = null;
        }
    }
}