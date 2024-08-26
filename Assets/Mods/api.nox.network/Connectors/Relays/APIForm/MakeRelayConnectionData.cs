using api.nox.network.Relays;
using Nox.CCK.Mods;

namespace api.nox.network
{
    public class MakeRelayConnectionData : ShareObject
    {
        // the protocol to use
        public RelayProtocol protocol;

        // address who will be connected to the relay
        [ShareObjectImport] public string relay_address;

        // is for check if the relay use the good master address (not check if the string is empty)
        [ShareObjectImport] public string master_address;

        // authentification data if you need to authentificate to the relay
        public RelayAuthentificationData authentificate;


        // data can't be serialized directly, so we need to import with base type and convert it
        [ShareObjectImport] public ShareObject SharedAuthentificate;
        [ShareObjectImport] public byte SharedProtocol;

        public void AfterImport()
        {
            authentificate = SharedAuthentificate?.Convert<RelayAuthentificationData>();
            protocol = (RelayProtocol)SharedProtocol;
        }
    }

    public enum RelayProtocol : byte
    {
        TCP = 0,
        UDP = 1
    }

    public class RelayAuthentificationData : ShareObject
    {
        // the token to authentificate
        [ShareObjectImport] public string token;

        // indicate if the token is a integrity token (for external users)
        [ShareObjectImport] public bool use_integrity_token;

        // the server address
        [ShareObjectImport] public string server_address;

        // the user id
        [ShareObjectImport] public uint user_id;
    }

    public class MakeRelayConnectionResponse : ShareObject
    {
        [ShareObjectExport] public bool IsSuccess;

        [ShareObjectExport] public string Error;

        public Relay Relay;

        [ShareObjectExport] public ShareObject SharedRelay;

        public void BeforeExport()
        {
            SharedRelay = Relay;
        }

        public void AfterExport()
        {
            SharedRelay = null;
        }
    }
}