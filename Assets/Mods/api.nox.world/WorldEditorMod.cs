#if UNITY_EDITOR
using System.Linq;
using Nox.CCK.Editor;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using UnityEngine.UIElements;

namespace api.nox.world
{
    public class WorldEditorMod : EditorModInitializer
    {
        internal WorldLoaderPanel _loader;
        internal WorldBuilderPanel _builder;
        private WorldPublisherPanel _publisher;
        internal EditorPanel _loaderpanel;
        internal EditorPanel _builderpanel;
        private EditorPanel _publisherpanel;
        internal EditorModCoreAPI _api;
        internal SimplyNetworkAPI NetworkAPI => _api.ModAPI.GetMod("network")?.GetMainClasses().OfType<ShareObject>().FirstOrDefault()?.Convert<SimplyNetworkAPI>();

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            _api = api;
            _loader = new WorldLoaderPanel(this);
            _builder = new WorldBuilderPanel(this);
            _publisher = new WorldPublisherPanel(this);

            _loaderpanel = api.PanelAPI.AddLocalPanel(_loader);
            _builderpanel = api.PanelAPI.AddLocalPanel(_builder);
            _publisherpanel = api.PanelAPI.AddLocalPanel(_publisher);
        }

        public void OnDispose()
        {

        }

        public void OnUpdateEditor()
        {
            _builder.OnUpdate();
            _publisher.OnUpdate();
        }

        internal bool HasOnePanelOpenned()
        {
            return _loaderpanel.IsActive() || _builderpanel.IsActive() || _publisherpanel.IsActive();
        }
    }
}
#endif