#if UNITY_EDITOR
using System.Collections.Generic;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Panels;
using UnityEngine.UIElements;

namespace dev.nox.development
{
    public class EventLogger : EditorModInitializer
    {
        public static EditorModCoreAPI CoreAPI;
        private EditorPanel buildPanel;
        private EventSubscription sublogs;

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            CoreAPI = api;
            var panel = new EventLoggerPanel();
            buildPanel = api.PanelAPI.AddLocalPanel(panel);
            sublogs = CoreAPI.EventAPI.Subscribe(null, (context) => panel.OnReceiveLog(context, true));
        }

        public void OnDispose()
        {
            CoreAPI.PanelAPI.RemoveLocalPanel(buildPanel);
            CoreAPI.EventAPI.Unsubscribe(sublogs);
            CoreAPI = null;
        }
    }

    public class EventLoggerPanel : EditorPanelBuilder
    {
        public string Id { get; } = "logger";
        public string Name { get; } = "Dev/Logger";
        public bool Hidded { get; } = false;
        internal VisualElement _root = new();

        public VisualElement OnOpenned(Dictionary<string, object> data)
        {
            _root.ClearBindings();
            _root.Clear();
            _root.Add(EventLogger.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("logger.uxml").CloneTree());
            _root.Q<Label>("version").text = "v" + EventLogger.CoreAPI.ModMetadata.GetVersion();

            _root.Q<Button>("clear").RegisterCallback<ClickEvent>(e =>
            {
                history.Clear();
                var log_div = _root.Q<VisualElement>("logs");
                log_div.Clear();
            });

            foreach (var context in history)
                OnReceiveLog(context, false);

            return _root;
        }

        public List<EventData> history = new();
        private uint maxLogs = 256;

        public void OnReceiveLog(EventData context, bool save = true)
        {
            if (save)
            {
                history.Add(context);
                while (history.Count > maxLogs) history.RemoveAt(0);
            }

            if (_root.childCount == 0) return;
            var log_div = _root.Q<VisualElement>("logs");
            var label = new Label($"[{context.Source.GetMetadata().GetId()}/{context.SourceChannel}] {context.EventName}");
            log_div.Add(label);

            while (log_div.childCount > maxLogs) log_div.RemoveAt(0);
        }

        public void OnClosed() { }
    }
}
#endif