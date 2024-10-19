using api.nox.network.Relays;
using Nox.CCK.Mods;

namespace api.nox.network
{
    public class MakeRelayConnectionData
    {
        // the protocol to use
        public RelayProtocol protocol;

        // address who will be connected to the relay
        public string relay_address;

        // is for check if the relay use the good master address (not check if the string is empty)
        public string master_address;

        // authentification data if you need to authentificate to the relay
        public RelayAuthentificationData authentificate;

        public string password;
        public string display_name;
    }

    public enum RelayProtocol : byte
    {
        TCP = 0,
        UDP = 1
    }

    public class RelayAuthentificationData
    {
        // the token to authentificate
        public string token;

        // indicate if the token is a integrity token (for external users)
        public bool use_integrity_token;

        // the server address
        public string server_address;

        // the user id
        public uint user_id;
    }

    public class MakeRelayConnectionResponse
    {
        public bool IsSuccess;

        public string Error;

        public Relay Relay;
    }
}