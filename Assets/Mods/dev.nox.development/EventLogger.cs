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
        private EditorPanel _buildPanel;
        private EventSubscription _subLogs;

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            CoreAPI = api;
            var panel = new EventLoggerPanel();
            _buildPanel = api.PanelAPI.AddLocalPanel(panel);
            _subLogs = CoreAPI.EventAPI.Subscribe(null, ctx => panel.OnReceiveLog(ctx));
        }

        public void OnDispose()
        {
            CoreAPI.PanelAPI.RemoveLocalPanel(_buildPanel);
            CoreAPI.EventAPI.Unsubscribe(_subLogs);
            CoreAPI = null;
        }
    }

    public class EventLoggerPanel : EditorPanelBuilder
    {
        public string GetId() => "logger";
        public string GetName() => "Dev/Logger";
        public bool IsHidden() => false;

        private readonly VisualElement _root = new();

        public VisualElement OnOpened(Dictionary<string, object> data)
        {
            _root.ClearBindings();
            _root.Clear();
            _root.Add(EventLogger.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("logger.uxml").CloneTree());
            _root.Q<Label>("version").text = "v" + EventLogger.CoreAPI.ModMetadata.GetVersion();

            _root.Q<Button>("clear").RegisterCallback<ClickEvent>(_ =>
            {
                _history.Clear();
                var logDiv = _root.Q<VisualElement>("logs");
                logDiv.Clear();
            });

            foreach (var context in _history)
                OnReceiveLog(context, false);

            return _root;
        }

        private readonly List<EventData> _history = new();
        private const uint MaxLogs = byte.MaxValue;

        public void OnReceiveLog(EventData context, bool save = true)
        {
            if (save)
            {
                _history.Add(context);
                while (_history.Count > MaxLogs) 
                    _history.RemoveAt(0);
            }

            if (_root.childCount == 0) return;
            var logDiv = _root.Q<VisualElement>("logs");
            var label = new Label($"[{context.Source.GetMetadata().GetId()}/{context.SourceChannel}] {context.EventName}");
            logDiv.Add(label);

            while (logDiv.childCount > MaxLogs) 
                logDiv.RemoveAt(0);
        }

        public void OnClosed() { }
    }
}
#endif