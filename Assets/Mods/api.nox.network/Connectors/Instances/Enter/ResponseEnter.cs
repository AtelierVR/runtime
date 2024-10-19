using System;
using api.nox.network.RelayInstances.Base;
using api.nox.network.Utils;
using Nox.CCK.Mods;
using Buffer = api.nox.network.Utils.Buffer;

namespace api.nox.network.RelayInstances.Enter
{
    public class ResponseEnter : InstanceResponse, ShareObject
    {
        public EnterResult Result;

        // if Result == Blacklisted
        [ShareObjectExport] public string Reason;
        [ShareObjectExport] public DateTime Expiration;

        // if Result in {Success, AlreadyConnected, PasswordRequired}
        public LocalPlayer Player;
        [ShareObjectExport] public byte MaxTps;

        public override bool FromBuffer(Buffer buffer)
        {
            var instanceId = buffer.ReadUShort();
            if (instanceId != InternalId) return false;
            Result = buffer.ReadEnum<EnterResult>();
            switch (Result)
            {
                case EnterResult.Refused:
                    if (buffer.length < 1) return false;
                    Reason = buffer.ReadString();
                    break;
                case EnterResult.Blacklisted:
                    if (buffer.length < 10) return false;
                    Reason = buffer.ReadString();
                    if (buffer.length < Reason.Length + 10) return false;
                    Expiration = buffer.ReadDateTime();
                    break;
                case EnterResult.Success:
                    Player = new LocalPlayer
                    {
                        RelayId = RelayId,
                        InternalId = InternalId,
                        Flags = buffer.ReadEnum<PlayerFlags>(),
                        Id = buffer.ReadUShort(),
                        DisplayName = buffer.ReadString(),
                        DateReference = buffer.ReadDateTime()
                    };
                    MaxTps = buffer.ReadByte();
                    break;
                default:
                    break;
            }

            return true;
        }

        public override string ToString() => $"{GetType().Name}[Result={Result}, Player={Player}]";

        public bool IsSuccess => Result == EnterResult.Success;

        [ShareObjectExport] public byte SharedResult;
        [ShareObjectExport] public Func<bool> SharedIsSuccess;

        public void BeforeExport()
        {
            SharedResult = (byte)Result;
            SharedIsSuccess = () => IsSuccess;
        }

        public void AfterImport()
        {
            Result = (EnterResult)SharedResult;
        }

    }
}