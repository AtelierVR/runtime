/*#if UNITY_EDITOR
using System.Collections.Generic;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Panels;
using UnityEngine.UIElements;
using System;
using System.Linq;

namespace api.nox.network.Editor
{
    public class NetworkEditor : EditorModInitializer
    {
        internal static EditorModCoreAPI CoreEditorAPI;
        private EditorPanel _cachePanel;
        private NetCachePanel _cacheBuildPanel;
        internal EditorPanel RelayPanel;
        // internal NetRelayPanel relaybuiledpanel;

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            CoreEditorAPI = api;
            _cacheBuildPanel = new NetCachePanel();
            _cachePanel = CoreEditorAPI.PanelAPI.AddLocalPanel(_cacheBuildPanel);
            // relaybuiledpanel = new NetRelayPanel();
            // relaypanel = CoreEditorAPI.PanelAPI.AddLocalPanel(relaybuiledpanel);
            NetCache.OnCacheSet.AddListener(OnSetCache);
            NetCache.OnCacheRemove.AddListener(OnRemovedCache);
            // RelayManager.OnSet.AddListener(OnSettedRelay);
            // RelayManager.OnRemove.AddListener(OnRemoveRelay);
        }

        private void OnSetCache(ICached set)
        {
            if (_cachePanel.IsActive())
                _cacheBuildPanel.UpdateCache(set, true);
        }

        private void OnRemovedCache(ICached removed)
        {
            if (_cachePanel.IsActive())
                _cacheBuildPanel.UpdateCache(removed, false);
        }

        // private void OnRemoveRelay(Relay removed)
        // {
        //     if (relaypanel.IsActive())
        //         relaybuiledpanel.UpdateRelay(removed, false);
        // }

        // private void OnSettedRelay(Relay setted)
        // {
        //     if (relaypanel.IsActive())
        //         relaybuiledpanel.UpdateRelay(setted, true);
        // }

        public void OnDispose()
        {
            CoreEditorAPI.PanelAPI.RemoveLocalPanel(_cachePanel);
            CoreEditorAPI.PanelAPI.RemoveLocalPanel(RelayPanel);
            NetCache.OnCacheSet.RemoveListener(OnSetCache);
            NetCache.OnCacheRemove.RemoveListener(OnRemovedCache);
            // RelayManager.OnSet.RemoveListener(OnSettedRelay);
            // RelayManager.OnRemove.RemoveListener(OnRemoveRelay);
            CoreEditorAPI = null;
        }

        // public void OnUpdateEditor()
        // {
        //     if (relaypanel.IsActive())
        //         relaybuiledpanel.OnGUI();
        // }
    }

    public class NetCachePanel : EditorPanelBuilder
    {
        public string GetId() => "cache";
        public string GetName() => "Network/Cache";
        public bool IsHidden() => false;
        private readonly VisualElement _root = new();


        public VisualElement Make(Dictionary<string, object> data)
        {
            _root.ClearBindings();
            _root.Clear();

            _root.Add(NetworkEditor.CoreEditorAPI.AssetAPI.GetAsset<VisualTreeAsset>("cache.uxml").CloneTree());
            _root.Q<Label>("version").text = "v" + NetworkEditor.CoreEditorAPI.ModMetadata.GetVersion();

            foreach (var cache in NetCache.Caches)
                UpdateCache(cache, true);

            return _root;
        }

        public void OnClosed()
        {
            _root.ClearBindings();
            _root.Clear();
        }

        public void UpdateCache(ICached value, bool isSet)
        {
            if (_root.childCount == 0) return;

            if (isSet)
                _root.Q<VisualElement>("notifications")
                    .Add(new Label(value.ToString()) { name = value.GetCacheKey() });
            else
                _root.Q<VisualElement>("notifications")
                    .Q<Label>(value.GetCacheKey())?.RemoveFromHierarchy();

            _root.Q<Label>("elements").text = LanguageManager.Get("network.cache.elements", NetCache.Count());
        }
    }


    /*public class NetRelayPanel : EditorPanelBuilder
    {
        public string Id { get; } = "relay";
        public string Name { get; } = "Network/Relay";
        public bool Hidded { get; } = false;
        internal VisualElement _root = new();


        public VisualElement OnOpenned(Dictionary<string, object> data)
        {
            _root.ClearBindings();
            _root.Clear();

            _root.Add(NetworkEditor.CoreEditorAPI.AssetAPI.GetAsset<VisualTreeAsset>("relay.uxml").CloneTree());
            _root.Q<Label>("version").text = "v" + NetworkEditor.CoreEditorAPI.ModMetadata.GetVersion();

            foreach (var relay in RelayManager.Cache)
                UpdateRelay(relay, true);

            return _root;
        }

        public void OnGUI()
        {
            if (_root.childCount == 0) return;

            foreach (var relay in RelayManager.Cache)
            {
                var element = _root.Q<VisualElement>("notifications").Q<Label>(relay.Id.ToString());
                if (element != null) OnUpdateDisplay(element, relay);
            }
        }

        private void OnUpdateDisplay(Label element, Relay relay)
        {
            element.text = $"{relay} ({relay.LastLatency?.GetLatency().ToString() ?? "N/A"}ms)";
        }

        public void OnClosed()
        {
            _root.ClearBindings();
            _root.Clear();
        }

        public void UpdateRelay(Relay value, bool isSet)
        {
            if (_root.childCount == 0) return;

            if (isSet)
            {
                var element = new Label(value.ToString()) { name = value.Id.ToString() };
                _root.Q<VisualElement>("notifications").Add(element);
                OnUpdateDisplay(element, value);
            }
            else _root.Q<VisualElement>("notifications")
                    .Q<Label>(value.Id.ToString())?.RemoveFromHierarchy();

            _root.Q<Label>("elements").text = LanguageManager.Get("network.relay.elements", new object[] { RelayManager.Cache.Count });
        }
    }#1#
}
#endif*/

