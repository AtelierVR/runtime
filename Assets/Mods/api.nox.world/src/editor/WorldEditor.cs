#if UNITY_EDITOR
using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Panels;

namespace api.nox.world
{
    public class WorldEditor : EditorModInitializer
    {
        private WorldLoaderPanel _loader;
        private static EditorPanel _loaderPanel;
        private static EditorPanel _builderPanel;
        private static EditorPanel _publisherPanel;
        private WorldPublisherPanel _publisher;
        internal static WorldBuilderPanel Builder;
        internal static EditorModCoreAPI CoreAPI;
        
        internal static MainModInitializer NetworkAPI 
            => CoreAPI.ModAPI
                .GetMod("network").GetMains()
                .FirstOrDefault();

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            CoreAPI = api;
            _loader = new WorldLoaderPanel();
            Builder = new WorldBuilderPanel();
            _publisher = new WorldPublisherPanel();

            _loaderPanel = api.PanelAPI.AddLocalPanel(_loader);
            _builderPanel = api.PanelAPI.AddLocalPanel(Builder);
            _publisherPanel = api.PanelAPI.AddLocalPanel(_publisher);
        }

        public void OnDisposeEditor()
        {
            Builder.Dispose();
        }

        public void OnUpdateEditor()
        {
            Builder.OnUpdate();
            _publisher.OnUpdate();
        }

        internal static bool HasOnePanelOpened() 
            => _loaderPanel.IsActive() 
               || _builderPanel.IsActive() 
               || _publisherPanel.IsActive();
    }
}
#endif