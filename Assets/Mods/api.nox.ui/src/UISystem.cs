using api.nox.ui.Mods.api.nox.ui.src.pages;
using api.nox.ui.widgets;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;

namespace api.nox.ui
{
    public class UISystem : MainModInitializer
    {
        internal static UISystem Instance;
        internal static MainModCoreAPI CoreAPI;

        [NoxPublic(NoxAccess.Read)] 
        public WidgetManager Widgets;
        
        
        [NoxPublic(NoxAccess.Read)] 
        public PageManager Pages;

        public void OnInitializeMain(MainModCoreAPI api)
        {
            Instance = this;
            CoreAPI = api;
            Widgets = new WidgetManager();
            Pages = new PageManager();
        }

        public void OnDisposeMain()
        {
            Widgets.Dispose();
            Widgets = null;
            Pages.Dispose();
            Pages = null;
            Instance = null;
            CoreAPI = null;
        }
    }
}