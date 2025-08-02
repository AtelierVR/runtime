#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Panels;
using Nox.CCK.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace dev.nox.development {
	public class ModDetails : EditorModInitializer {
		internal static EditorModCoreAPI CoreAPI;
		private       EditorPanel      _buildPanel;

		public void OnInitializeEditor(EditorModCoreAPI api) {
			CoreAPI = api;
			var panel = new ModDetailsPanel();
			_buildPanel = api.PanelAPI.AddLocalPanel(panel);
		}

		public void OnDispose() {
			CoreAPI.PanelAPI.RemoveLocalPanel(_buildPanel);
			CoreAPI = null;
		}
	}

	public class ModDetailsPanel : IEditorPanelBuilder {
		public string GetId()
			=> "mod_details";

		public string GetName()
			=> "Dev/Mod Details";

		public bool IsHidden()
			=> false;

		private readonly VisualElement _root = new();
		private DateTime _lastUpdate = DateTime.MinValue;
		private bool _autoRefresh = true;
		private bool _showDisabled = true;

		private class ModUserData {
			private readonly string _modId;
			private readonly string _version;
			internal DateTime LastModified { get; set; } = DateTime.MinValue;

			internal ModUserData(object mod) {
				var meta = (dynamic)mod.GetType().GetMethod("GetMetadata").Invoke(mod, null);
				_modId = meta.GetId();
				_version = meta.GetVersion().ToString(); // Convert Version to string
			}

			public bool Equals(string modId) => _modId == modId;

			public bool NeedsUpdate(object mod) {
				var meta = (dynamic)mod.GetType().GetMethod("GetMetadata").Invoke(mod, null);
				return _version != meta.GetVersion().ToString() || 
				       DateTime.Now - LastModified > TimeSpan.FromSeconds(5);
			}

			public override string ToString() => _modId;
		}

		public VisualElement Make(Dictionary<string, object> data) {
			_root.ClearBindings();
			_root.Clear();
			
			// Load the main UXML template
			_root.Add(ModDetails.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("mod_details.uxml").CloneTree());
			
			// Set version
			_root.Q<Label>("version").text = "v" + EventLogger.CoreAPI.ModMetadata.GetVersion();
			
			// Setup event handlers
			SetupEventHandlers();
			
			// Initial refresh
			_lastUpdate = DateTime.MinValue;
			RefreshModsList();
			
			return _root;
		}

		private void SetupEventHandlers() {
			_root.Q<Button>("refresh-button").clicked += RefreshModsList;
			
			var autoRefreshToggle = _root.Q<Toggle>("auto-refresh");
			autoRefreshToggle.value = _autoRefresh;
			autoRefreshToggle.RegisterValueChangedCallback(evt => _autoRefresh = evt.newValue);
			
			var showDisabledToggle = _root.Q<Toggle>("show-disabled");
			showDisabledToggle.value = _showDisabled;
			showDisabledToggle.RegisterValueChangedCallback(evt => {
				_showDisabled = evt.newValue;
				RefreshModsList();
			});
		}

		private VisualElement GetModElement(string modId) =>
			_root.Q<VisualElement>("mods-list")
				?.Children()
				.FirstOrDefault(c => c.userData is ModUserData data && data.Equals(modId));

		public void OnUpdate() {
			if (!_autoRefresh) return;
			if (DateTime.Now - _lastUpdate < TimeSpan.FromSeconds(2)) return;
			
			var mods = ModDetails.CoreAPI.ModAPI.GetMods();
			var filteredMods = _showDisabled ? mods : mods.Where(m => m.IsLoaded()).ToArray();
			
			// Update existing items or add new ones
			foreach (var mod in filteredMods) {
				var meta = (dynamic)mod.GetType().GetMethod("GetMetadata").Invoke(mod, null);
				var child = GetModElement(meta.GetId());
				if (child != null) {
					var userData = child.userData as ModUserData;
					if (userData?.NeedsUpdate(mod) == true) {
						UpdateModItem(child, mod);
						userData.LastModified = DateTime.Now;
					}
				} else {
					OnModAdded(mod);
				}
			}
			
			// Remove items for mods that no longer exist or are filtered out
			var modsList = _root.Q<VisualElement>("mods-list");
			var toRemove = new List<VisualElement>();
			foreach (var child in modsList.Children()) {
				if (child.userData is ModUserData userData) {
					var modExists = filteredMods.Any(m => {
						var meta = (dynamic)m.GetType().GetMethod("GetMetadata").Invoke(m, null);
						return userData.Equals(meta.GetId());
					});
					if (!modExists) toRemove.Add(child);
				}
			}
			toRemove.ForEach(child => OnModRemoved(child));
			
			// Update mod count
			var enabledCount = mods.Count(m => m.IsLoaded());
			_root.Q<Label>("mod-count").text = $"Mods: {enabledCount}/{mods.Length} enabled";
			
			_lastUpdate = DateTime.Now;
		}

		private void RefreshModsList() {
			var mods = ModDetails.CoreAPI.ModAPI.GetMods();
			var modsList = _root.Q<VisualElement>("mods-list");
			
			// Clear existing items
			modsList.Clear();
			
			// Update mod count
			var enabledCount = mods.Count(m => m.IsLoaded());
			_root.Q<Label>("mod-count").text = $"Mods: {enabledCount}/{mods.Length} enabled";
			
			// Filter mods if needed
			var filteredMods = _showDisabled ? mods : mods.Where(m => m.IsLoaded()).ToArray();
			
			foreach (var mod in filteredMods) {
				OnModAdded(mod);
			}
			
			_lastUpdate = DateTime.Now;
		}

		private void OnModAdded(object mod) {
			var modsList = _root.Q<VisualElement>("mods-list");
			if (modsList == null) return;
			
			var meta = (dynamic)mod.GetType().GetMethod("GetMetadata").Invoke(mod, null);
			var child = GetModElement(meta.GetId());
			if (child != null) {
				UpdateModItem(child, mod);
				return;
			}

			var modItemTemplate = ModDetails.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("mod_item.uxml");
			child = modItemTemplate.CloneTree();
			
			var userData = new ModUserData(mod);
			child.userData = userData;
			
			UpdateModItem(child, mod);
			modsList.Add(child);
			userData.LastModified = DateTime.Now;
		}

		private void OnModRemoved(VisualElement child) {
			_root.Q<VisualElement>("mods-list")?.Remove(child);
			child.RemoveFromHierarchy();
		}

		private void UpdateModItem(VisualElement item, object mod) {
			if (item == null || mod == null) return;
			
			var meta = (dynamic)mod.GetType().GetMethod("GetMetadata").Invoke(mod, null);
			
			// Set basic info
			item.Q<Label>("mod-id").text = meta.GetId();
			item.Q<Label>("mod-version").text = "v" + meta.GetVersion();
			item.Q<Label>("mod-name").text = meta.GetName();
			item.Q<Label>("mod-license").text = meta.GetLicense();
			
			// Setup status indicators
			SetupStatusIndicators(item, mod);
			
			// Setup provides list
			SetupProvidesList(item, meta);
			
			// Setup entry points
			SetupEntryPoints(item, mod);
			
			// Setup select button (only once)
			var selectButton = item.Q<Button>("select-button");
			if (selectButton != null && selectButton.userData == null) {
				selectButton.userData = "initialized";
				selectButton.clicked += () => {
					var manifestPath = (string)mod.GetType().GetMethod("GetData").MakeGenericMethod(typeof(string)).Invoke(mod, new object[] { "manifest", "" });
					if (File.Exists(manifestPath))
						EditorUtility.RevealInFinder(manifestPath);
					else
						EditorUtility.DisplayDialog("Error", "Manifest file not found!", "Ok");
				};
			}
		}

		private void SetupStatusIndicators(VisualElement item, object mod) {
			var isLoaded = (bool)mod.GetType().GetMethod("IsLoaded").Invoke(mod, null);
			var mainCount = ((Array)mod.GetType().GetMethod("GetMains").Invoke(mod, null)).Length;
			var clientCount = ((Array)mod.GetType().GetMethod("GetClients").Invoke(mod, null)).Length;
			var serverCount = ((Array)mod.GetType().GetMethod("GetServers").Invoke(mod, null)).Length;
			var editorCount = ((Array)mod.GetType().GetMethod("GetEditors").Invoke(mod, null)).Length;
			var customEntries = (Array)mod.GetType().GetMethod("GetCustomEntries").Invoke(mod, null);
			
			// Update status indicators visibility and colors
			var statusLoaded = item.Q("status-loaded");
			statusLoaded.style.backgroundColor = isLoaded ? new Color(0, 1, 0) : new Color(0.5f, 0.5f, 0.5f);
			
			item.Q("status-main").style.display = mainCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
			item.Q("status-client").style.display = clientCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
			item.Q("status-server").style.display = serverCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
			item.Q("status-editor").style.display = editorCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
			item.Q("status-custom").style.display = customEntries.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
		}

		private void SetupProvidesList(VisualElement item, dynamic meta) {
			var providesList = item.Q<VisualElement>("provides-list");
			var provides = meta.GetProvides();
			
			providesList.Clear();
			if (provides.Length == 0) {
				providesList.Add(new Label("None") { style = { color = new Color(0.7f, 0.7f, 0.7f) } });
			} else {
				foreach (var provide in provides) {
					var label = new Label($"• {provide}");
					label.style.fontSize = 11;
					label.style.color = new Color(0.8f, 0.8f, 0.8f);
					providesList.Add(label);
				}
			}
		}

		private void SetupEntryPoints(VisualElement item, object mod) {
			SetupEntryPointSection(item, "main", mod.GetType().GetMethod("GetMains").Invoke(mod, null) as Array);
			SetupEntryPointSection(item, "client", mod.GetType().GetMethod("GetClients").Invoke(mod, null) as Array);
			SetupEntryPointSection(item, "server", mod.GetType().GetMethod("GetServers").Invoke(mod, null) as Array);
			SetupEntryPointSection(item, "editor", mod.GetType().GetMethod("GetEditors").Invoke(mod, null) as Array);
			
			// Setup custom entries
			var customEntries = mod.GetType().GetMethod("GetCustomEntries").Invoke(mod, null) as Array;
			var customContainer = item.Q<VisualElement>("custom-entries");
			var customList = item.Q<VisualElement>("custom-list");
			
			if (customEntries.Length == 0) {
				customContainer.style.display = DisplayStyle.None;
			} else {
				customList.Clear();
				foreach (var customEntry in customEntries) {
					var customArray = mod.GetType().GetMethod("GetCustom").Invoke(mod, new object[] { customEntry }) as Array;
					var isEnabled = (bool)mod.GetType().GetMethod("IsCustomEnabled").Invoke(mod, new object[] { customEntry });
					
					var entryLabel = new Label($"{customEntry} ({(isEnabled ? "enabled" : "disabled")}):");
					entryLabel.style.fontSize = 11;
					entryLabel.style.color = isEnabled ? new Color(1, 1, 1) : new Color(0.6f, 0.6f, 0.6f);
					customList.Add(entryLabel);
					
					foreach (var entry in customArray) {
						var label = new Label($"  • {entry}");
						label.style.fontSize = 10;
						label.style.color = new Color(0.8f, 0.8f, 0.8f);
						customList.Add(label);
					}
				}
			}
		}

		private void SetupEntryPointSection(VisualElement item, string sectionName, Array entries) {
			var container = item.Q<VisualElement>($"{sectionName}-entries");
			var list = item.Q<VisualElement>($"{sectionName}-list");
			
			if (entries.Length == 0) {
				container.style.display = DisplayStyle.None;
			} else {
				list.Clear();
				foreach (var entry in entries) {
					var label = new Label($"• {entry}");
					label.style.fontSize = 10;
					label.style.color = new Color(0.8f, 0.8f, 0.8f);
					list.Add(label);
				}
			}
		}
	}
}
#endif

