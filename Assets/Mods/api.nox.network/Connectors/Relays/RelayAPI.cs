using System;
using api.nox.network.RelayInstances;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.network.Relays
{
    public class RelayAPI : ShareObject
    {
        internal void Update()
        {
            RelayManager.Update();
            RelayInstanceManager.Update();
        }
        internal void Dispose()
        {
            RelayManager.Dispose();
            RelayInstanceManager.Dispose();
        }

        public Relay GetRelay(ushort id) => RelayManager.Get(id);
        public bool HasRelay(ushort id) => RelayManager.Has(id);

        public Relay GetRelay(string address) => RelayManager.Cache.Find(r => r.UserData is int i && i == Animator.StringToHash(address));
        public bool HasRelay(string address) => GetRelay(address) != null;

        public RelayInstance GetInstance(ushort relayId, ushort instanceId) => RelayInstanceManager.Get(instanceId, relayId);

        public async UniTask<MakeRelayConnectionResponse> MakeConnection(MakeRelayConnectionData data)
        {
            // Find the best connector for the protocol
            var connector = ConnectorFromEnum(data.protocol);
            if (connector == null) return new() { IsSuccess = false, Error = "Invalid protocol" };

            // Find the relay
            var uri = await Gateway.FindGatewayRelay(data.relay_address);
            if (uri == null) return new() { IsSuccess = false, Error = "Failed to find relay" };

            // Connect to the relay
            var relay = new Relay(connector) { UserData = Animator.StringToHash(data.relay_address) };
            var con_s = relay.Connect(uri.Host, (ushort)uri.Port);
            if (!con_s) return new() { IsSuccess = false, Error = "Failed to connect to relay" };

            // Handshake with the relay
            var hand_s = await relay.RequestHandshake();
            if (hand_s == null) return new() { IsSuccess = false, Error = "Failed to handshake with relay" };

            // check use good master address
            if (!string.IsNullOrEmpty(data.master_address))
            {
                var status_s = await relay.RequestStatus();
                if (status_s == null || status_s.MasterAddress != data.master_address)
                    return new() { IsSuccess = false, Error = "Invalid master address (expected: " + data.master_address + ", got: " + status_s.MasterAddress + ")" };
            }

            // Authentificate to the relay
            if (data.authentificate != null)
            {
                var auth_s = await relay.RequestAuthentification(new()
                {
                    AccessToken = data.authentificate.token,
                    ServerAddress = data.authentificate.server_address,
                    UserId = data.authentificate.user_id,
                    Flags = data.authentificate.use_integrity_token ? Relays.Auth.AuthFlags.UseIntegrity : Relays.Auth.AuthFlags.None,
                });
                if (auth_s == null)
                    return new() { IsSuccess = false, Error = "Failed to authentificate to relay" };

                // Check if the authentification is valid
                if (!auth_s.IsSuccess)
                    return auth_s.Result switch
                    {
                        Relays.Auth.AuthResult.InvalidToken => new() { IsSuccess = false, Error = "Invalid token" },
                        Relays.Auth.AuthResult.Blacklisted => new() { IsSuccess = false, Error = "Blacklisted: " + auth_s.Reason },
                        Relays.Auth.AuthResult.CannotContactAuthServer => new() { IsSuccess = false, Error = "Cannot contact auth server" },
                        Relays.Auth.AuthResult.NotReady => new() { IsSuccess = false, Error = "Relay not ready" },
                        _ => new() { IsSuccess = false, Error = "Unknown error" },
                    };
            }

            // Return the relay
            return new() { IsSuccess = true, Relay = relay };
        }

        public static IConnector ConnectorFromEnum(RelayProtocol protocol) => protocol switch
        {
            RelayProtocol.TCP => new TcpConnector(),
            RelayProtocol.UDP => new UdpConnector(),
            _ => null,
        };
    }
}