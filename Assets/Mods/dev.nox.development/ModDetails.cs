#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Panels;
using Nox.CCK.Utils;
using UnityEngine.UIElements;

namespace dev.nox.development
{
    public class ModDetails : EditorModInitializer
    {
        public static EditorModCoreAPI CoreAPI;
        private EditorPanel buildPanel;

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            Logger.Log("ModDetails initialized");
            CoreAPI = api;
            var panel = new ModDetailsPanel();
            buildPanel = api.PanelAPI.AddLocalPanel(panel);
        }

        public void OnDispose()
        {
            CoreAPI.PanelAPI.RemoveLocalPanel(buildPanel);
            CoreAPI = null;
        }
    }

    public class ModDetailsPanel : EditorPanelBuilder
    {
        public string Id { get; } = "mod_details";
        public string Name { get; } = "Dev/Mod Details";
        public bool Hidded { get; } = false;
        private VisualElement _root = new();

        public VisualElement OnOpenned(Dictionary<string, object> data)
        {
            _root.ClearBindings();
            _root.Clear();
            _root.Add(ModDetails.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("mod_details.uxml").CloneTree());
            _root.Q<Label>("version").text = "v" + EventLogger.CoreAPI.ModMetadata.GetVersion();
            lastUpdate = DateTime.MinValue;
            UpdateContent();
            return _root;
        }

        public void OnGUI() => UpdateContent(true);
        private DateTime lastUpdate = DateTime.Now;

        void UpdateContent(bool a = false)
        {
            if (DateTime.Now - lastUpdate < TimeSpan.FromSeconds(1)) return;
            lastUpdate = DateTime.Now;
            var element = new VisualElement();
            var mods = ModDetails.CoreAPI.ModAPI.GetMods();

            element.Add(new Label($"Mods ({mods.Length})"));

            foreach (var mod in mods)
            {
                var meta = mod.GetMetadata();
                var modElement = new VisualElement();
                modElement.AddToClassList("mod");
                modElement.Add(new Label($" - {meta.GetId()} v{meta.GetVersion()}"));
                modElement.Add(new Label($"   - {meta.GetName()} ({meta.GetLicense()})"));
                modElement.Add(new Label($"   - Provides ({meta.GetProvides().Length})"));
                foreach (var provide in meta.GetProvides())
                    modElement.Add(new Label($"     - {provide}"));
                modElement.Add(new Label($"   - Enabled: {mod.IsLoaded()}"));
                modElement.Add(new Label($"     - main {(mod.IsMainEnabled() ? "enabled" : "disabled")}"));
                foreach (var main in mod.GetMains())
                    modElement.Add(new Label($"       - {main}"));
                modElement.Add(new Label($"     - client {(mod.IsClientEnabled() ? "enabled" : "disabled")}"));
                foreach (var main in mod.GetClients())
                    modElement.Add(new Label($"       - {main}"));
                modElement.Add(new Label($"     - server {(mod.IsServerEnabled() ? "enabled" : "disabled")}"));
                foreach (var main in mod.GetServers())
                    modElement.Add(new Label($"       - {main}"));
                modElement.Add(new Label($"     - editor {(mod.IsEditorEnabled() ? "enabled" : "disabled")}"));
                foreach (var main in mod.GetEditors())
                    modElement.Add(new Label($"       - {main}"));
                foreach (var custom in mod.GetCustomEntries())
                {
                    modElement.Add(
                        new Label($"     - {custom} {(mod.IsCustomEnabled(custom) ? "enabled" : "disabled")}"));
                    foreach (var main in mod.GetCustom(custom))
                        modElement.Add(new Label($"         - {main}"));
                }

                element.Add(modElement);
            }

            var logDiv = _root.Q<VisualElement>("logs");
            logDiv.Clear();
            logDiv.Add(element);
        }
    }
}
#endif