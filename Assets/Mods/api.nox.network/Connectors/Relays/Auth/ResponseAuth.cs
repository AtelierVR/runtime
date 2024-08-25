using System;
using api.nox.network.Relays.Base;
using Buffer = api.nox.network.Utils.Buffer;

namespace api.nox.network.Relays.Auth
{
    public class ResponseAuth : RelayResponse
    {
        public AuthResult Result;

        // if Result == Blacklisted
        public string Reason;
        public DateTime Expiration;

        // if Result == Success
        public uint UserId;
        public string DisplayName;
        public string ServerAddress;

        public override bool FromBuffer(Buffer buffer)
        {
            if (buffer.length < 1) return false;
            Result = (AuthResult)buffer.ReadByte();
            switch (Result)
            {
                case AuthResult.Blacklisted:
                    // 2 (string length) + 8 (DateTime) = 10
                    if (buffer.length < 10) return false;
                    Reason = buffer.ReadString();
                    if (buffer.length < Reason.Length + 10) return false;
                    Expiration = buffer.ReadDateTime();
                    break;
                case AuthResult.Success:
                    // 10 = 4 (uint) + 2 (ushort) + 4 (uint)
                    if (buffer.length < 10) return false;
                    UserId = buffer.ReadUInt();
                    DisplayName = buffer.ReadString();
                    if (buffer.length < DisplayName.Length + 10) return false;
                    ServerAddress = buffer.ReadString();
                    break;
            }

            return true;
        }

        public override string ToString() =>
            $"{GetType().Name}[Result={Result}]";

        public bool IsSuccess => Result == AuthResult.Success;
    }
}