using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyMakeRelayConnectionData : ShareObject
    {

        // address who will be connected to the relay
        [ShareObjectExport] public string relay_address;

        // is for check if the relay use the good master address (not check if the string is empty)
        [ShareObjectExport] public string master_address;

        // data can't be serialized directly, so we need to import with base type and convert it
        [ShareObjectExport] public ShareObject SharedAuthentificate;
        [ShareObjectExport] public byte SharedProtocol;


        // authentification data if you need to authentificate to the relay
        public SimplyRelayAuthentificationData authentificate;
        // the protocol to use
        public SimplyRelayProtocol protocol;

        public void BeforeExport()
        {
            SharedAuthentificate = authentificate;
            SharedProtocol = (byte)protocol;
        }

        public void AfterExport()
        {
            SharedAuthentificate = null;
            SharedProtocol = 0;
        }

    }

    public enum SimplyRelayProtocol : byte
    {
        TCP = 0,
        UDP = 1
    }

    public class SimplyRelayAuthentificationData : ShareObject
    {
        // the token to authentificate
        [ShareObjectExport] public string token;

        // indicate if the token is a integrity token (for external users)
        [ShareObjectExport] public bool use_integrity_token;

        // the server address
        [ShareObjectExport] public string server_address;

        // the user id
        [ShareObjectExport] public uint user_id;
    }
}