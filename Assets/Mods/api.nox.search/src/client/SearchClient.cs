using System.Linq;
using api.nox.search.client;
using api.nox.search.widget;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace api.nox.search
{
    public class SearchClient : ClientModInitializer
    {
        internal static MainModInitializer UISystem
            => SearchSystem.CoreAPI.ModAPI
                .GetMod("ui").GetMains()
                .FirstOrDefault();

        private SearchWidget _searchWidget;

        public void OnInitializeClient(ClientModCoreAPI api)
        {
            SearchPage.Listen();
            _searchWidget = new SearchWidget();
        }

        public void OnDisposeClient()
        {
            SearchPage.StopListen();
            _searchWidget?.Dispose();
            _searchWidget = null;
        }
    }
}