using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace api.nox.server
{
    public class ServerClient : ClientModInitializer
    {
        internal static MainModInitializer UISystem
            => ServerSystem.CoreAPI.ModAPI
                .GetMod("ui").GetMains()
                .FirstOrDefault();

        private ServerWidget _serverWidget;

        public void OnInitializeClient(ClientModCoreAPI api)
        {
            _serverWidget = new ServerWidget();
        }

        public void OnDisposeClient()
        {
            _serverWidget.Dispose();
            _serverWidget = null;
        }
    }
}