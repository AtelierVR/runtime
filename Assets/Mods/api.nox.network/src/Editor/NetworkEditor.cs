#if UNITY_EDITOR
using System.Collections.Generic;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Panels;
using UnityEngine.UIElements;

namespace api.nox.network.Editor
{
    public class NetworkEditor : EditorModInitializer
    {
        internal static EditorModCoreAPI CoreEditorAPI;
        internal EditorPanel cachepanel;
        internal NetCachePanel cachebuiledpanel;
        internal EditorPanel relaypanel;
        // internal NetRelayPanel relaybuiledpanel;

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            CoreEditorAPI = api;
            cachebuiledpanel = new NetCachePanel();
            cachepanel = CoreEditorAPI.PanelAPI.AddLocalPanel(cachebuiledpanel);
            // relaybuiledpanel = new NetRelayPanel();
            // relaypanel = CoreEditorAPI.PanelAPI.AddLocalPanel(relaybuiledpanel);
            NetCache.OnCacheSet.AddListener(OnSettedCache);
            NetCache.OnCacheRemove.AddListener(OnRemovedCache);
            // RelayManager.OnSet.AddListener(OnSettedRelay);
            // RelayManager.OnRemove.AddListener(OnRemoveRelay);
        }

        private void OnSettedCache(ICached setted)
        {
            if (cachepanel.IsActive())
                cachebuiledpanel.UpdateCache(setted, true);
        }

        private void OnRemovedCache(ICached removed)
        {
            if (cachepanel.IsActive())
                cachebuiledpanel.UpdateCache(removed, false);
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
            CoreEditorAPI.PanelAPI.RemoveLocalPanel(cachepanel);
            CoreEditorAPI.PanelAPI.RemoveLocalPanel(relaypanel);
            NetCache.OnCacheSet.RemoveListener(OnSettedCache);
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
        public string Id { get; } = "cache";
        public string Name { get; } = "Network/Cache";
        public bool Hidded { get; } = false;
        internal VisualElement _root = new();


        public VisualElement OnOpenned(Dictionary<string, object> data)
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
            else _root.Q<VisualElement>("notifications")
                    .Q<Label>(value.GetCacheKey())?.RemoveFromHierarchy();

            _root.Q<Label>("elements").text = LanguageManager.Get("network.cache.elements", new object[] { NetCache.Count() });
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
    }*/

}
#endif