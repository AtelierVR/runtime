#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Panels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;

namespace dev.nox.development {
	public class Performances : EditorModInitializer {
		internal static EditorModCoreAPI    CoreAPI;
		private         EditorPanel         _buildPanel;
		private         EventSubscription[] _events = Array.Empty<EventSubscription>();
		private         PerformancePanel    _panel;

		internal static UnityEvent<Mod> ModLoadedEvent   = new();
		internal static UnityEvent<Mod> ModUnloadedEvent = new();

		public void OnInitializeEditor(EditorModCoreAPI api) {
			CoreAPI = api;

			_events = new[] {
				api.EventAPI.Subscribe("mod_loaded", ctx => OnModLoaded(ctx.TryGet(0, out Mod m) ? m : null)),
				api.EventAPI.Subscribe("mod_unloaded", ctx => OnModUnloaded(ctx.TryGet(0, out Mod m) ? m : null)),
			};

			foreach (var m in api.ModAPI.GetMods())
				OnModLoaded(m);

			_panel      = new PerformancePanel();
			_buildPanel = api.PanelAPI.AddLocalPanel(_panel);
		}

		public void OnDisposeEditor() {
			CoreAPI.PanelAPI.RemoveLocalPanel(_buildPanel);
			foreach (var subscription in _events)
				CoreAPI.EventAPI.Unsubscribe(subscription);
			_events = Array.Empty<EventSubscription>();
			_panel?.OnHidden();
			CoreAPI = null;
		}

		private void OnModLoaded(Mod mod) {
			if (mod == null) return;
			ModLoadedEvent.Invoke(mod);
		}

		private void OnModUnloaded(Mod mod) {
			if (mod == null) return;
			ModUnloadedEvent.Invoke(mod);
		}

		public void OnUpdateEditor() {
			_panel?.Update();
		}
	}

	public class PerformancePanel : IEditorPanelBuilder {
		public string GetId()
			=> "performances";

		public string GetName()
			=> "Dev/Performances";

		public bool IsHidden()
			=> false;

		private readonly VisualElement                  _root = new();
		private          PerformanceMonitor             _graphicMonitor;
		private          List<PerModPerformanceMonitor> _perModMonitors = new();

		public VisualElement Make(Dictionary<string, object> data) {
			_root.ClearBindings();
			_root.Clear();
			_root.Add(Performances.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("perfs.uxml").CloneTree());
			_root.Q<Label>("version").text = "v" + Performances.CoreAPI.ModMetadata.GetVersion();
			return _root;
		}

		public void Update() {
			_graphicMonitor?.Update();
			foreach (var monitor in _perModMonitors)
				monitor?.Update();
		}

		public void OnHidden() {
			Performances.ModLoadedEvent.RemoveListener(OnModLoaded);
			Performances.ModUnloadedEvent.RemoveListener(OnModUnloaded);
			foreach (var monitor in _perModMonitors)
				monitor.Dispose();
			_perModMonitors.Clear();
			_graphicMonitor?.Dispose();
			_graphicMonitor = null;
		}

		public void OnVisible() {
			_graphicMonitor = new PerformanceMonitor(_root);
			foreach (var mod in Performances.CoreAPI.ModAPI.GetMods())
				_perModMonitors.Add(new PerModPerformanceMonitor(mod.GetMetadata().GetId(), _root));
			Performances.ModLoadedEvent.AddListener(OnModLoaded);
			Performances.ModUnloadedEvent.AddListener(OnModUnloaded);
		}

		private void OnModLoaded(Mod mod) {
			var monitor = _perModMonitors.FirstOrDefault(m => m.ModId == mod.GetMetadata().GetId());
			if (monitor == null) return;
			_perModMonitors.Add(new PerModPerformanceMonitor(mod.GetMetadata().GetId(), _root));
		}

		private void OnModUnloaded(Mod mod) {
			var monitor = _perModMonitors.FirstOrDefault(m => m.ModId == mod.GetMetadata().GetId());
			if (monitor == null) return;
			_perModMonitors.Remove(monitor);
			monitor.Dispose();
		}
	}

	public class PerModPerformanceMonitor {
		public readonly  string        ModId;
		private          VisualElement _instance;
		private readonly Foldout       _foldout;
		private          VisualElement _data;
		private          VisualElement _graphContainer;
		private          Image         _graphImage;
		private          Texture2D     _graphTexture;


		private          DateTime                                        _lastLabelUpdate   = DateTime.UtcNow;
		private          DateTime                                        _lastGraphicUpdate = DateTime.UtcNow;
		private readonly Dictionary<string, List<float>>                 _fpsCurrent        = new();
		private readonly Dictionary<string, List<(float, float, float)>> _fpsHistory        = new();

		private const int   MaxHistoryPoints      = 256;
		private const float UpdateGraphicInterval = 1f / MaxHistoryPoints * 10;
		private const float UpdateLabelInterval   = 0.5f;

		public Mod Mod
			=> Performances.CoreAPI.ModAPI.GetMod(ModId);

		internal PerModPerformanceMonitor(string modId, VisualElement root) {
			ModId     = modId;
			_instance = Performances.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("mods-prefs.uxml").CloneTree();
			root.Q<VisualElement>("mods").Add(_instance);
			_foldout        = _instance.Q<Foldout>();
			_foldout.text   = modId;
			_data           = _instance.Q<VisualElement>("data");
			_graphContainer = _instance.Q<VisualElement>("mod-graph");
			Initialize();
		}

		private void Initialize() {
			if (_graphContainer == null) return;

			_graphContainer.Clear();

			// Get initial container dimensions
			var containerWidth  = Mathf.Max(1, (int)_graphContainer.layout.width);
			var containerHeight = Mathf.Max(1, (int)_graphContainer.layout.height);

			// If the layout isn't ready yet, use some reasonable defaults
			if (containerWidth <= 1 || containerHeight <= 1) {
				containerWidth  = 512;
				containerHeight = 256;
			}

			// Create the texture for the graph
			_graphTexture = new Texture2D(containerWidth, containerHeight, TextureFormat.RGBA32, false) {
				filterMode = FilterMode.Point
			};

			// Create the image element that will display the texture
			_graphImage = new Image {
				image = _graphTexture,
				style = {
					width      = new StyleLength(Length.Percent(100)),
					height     = new StyleLength(Length.Percent(100)),
					flexGrow   = 1,
					flexShrink = 1
				}
			};

			_graphContainer.Add(_graphImage);

			// Add legend
			var legend = new VisualElement {
				style = {
					position        = Position.Absolute,
					top             = 5,
					right           = 5,
					backgroundColor = new StyleColor(new Color(0, 0, 0, 0.5f)),
					paddingBottom   = 5,
					paddingLeft     = 5,
					paddingRight    = 5,
					paddingTop      = 5,
					flexDirection   = FlexDirection.Column
				}
			};

			_graphContainer.Add(legend);

			// Initial draw of the graph
			PerformanceMonitor.Fill(_graphTexture);
		}

		private void CheckAndResizeTexture() {
			if (!_graphTexture || _graphContainer == null || !_foldout.value) return;

			// Get current container dimensions
			var containerWidth  = (int)_graphContainer.layout.width;
			var containerHeight = (int)_graphContainer.layout.height;

			// Only resize if dimensions are valid and different from current texture
			if (containerWidth      <= 0 || containerHeight                   <= 0) return;
			if (_graphTexture.width == containerWidth && _graphTexture.height == containerHeight) return;

			// Recreate texture with new dimensions
			if (_graphTexture) {
				UnityEngine.Object.DestroyImmediate(_graphTexture);
			}

			_graphTexture = new Texture2D(containerWidth, containerHeight, TextureFormat.RGBA32, false) {
				filterMode = FilterMode.Point
			};

			// Update the image with the new texture
			if (_graphImage != null) {
				_graphImage.image = _graphTexture;
			}

			// Redraw everything
			UpdateGraphic();
		}


		internal void Update() {
			var mod       = Mod;
			var profilers = mod.GetPerformances();
			Array.Sort(profilers, (p1, p2) => string.Compare(p1.GetName(), p2.GetName(), StringComparison.Ordinal));

			CheckAndResizeTexture();

			foreach (var profiler in profilers) {
				if (!_fpsCurrent.ContainsKey(profiler.GetName()))
					_fpsCurrent[profiler.GetName()] = new List<float>();
				_fpsCurrent[profiler.GetName()].Add(Mathf.Abs((float)profiler.Duration.TotalMilliseconds));
			}

			if ((DateTime.UtcNow - _lastLabelUpdate).TotalSeconds >= UpdateLabelInterval) {
				_lastLabelUpdate = DateTime.UtcNow;
				UpdateLabels(mod, profilers);
			}

			if ((DateTime.UtcNow - _lastGraphicUpdate).TotalSeconds >= UpdateGraphicInterval) {
				_lastGraphicUpdate = DateTime.UtcNow;

				// Calculate FPS
				var fpsData = _fpsCurrent.Select(kvp => (kvp.Key, kvp.Value.Average(), kvp.Value.Min(), kvp.Value.Max())).ToList();
				_fpsCurrent.Clear();

				// Update FPS history
				foreach (var (name, avg, min, max) in fpsData) {
					if (!_fpsHistory.ContainsKey(name))
						_fpsHistory[name] = new List<(float, float, float)>();
					_fpsHistory[name].Add((min, avg, max));

					if (_fpsHistory[name].Count > MaxHistoryPoints)
						_fpsHistory[name].RemoveAt(0);
				}

				UpdateGraphic();
			}
		}

		private bool CanUpdate() {
			return _foldout is { value: true };
		}

		private void UpdateGraphic() {
			if (!_graphTexture || _instance == null || !CanUpdate()) return;

			// Clear the texture with a dark background
			PerformanceMonitor.Fill(_graphTexture);

			PerformanceMonitor.DrawGrid(_graphTexture);

			var maxValue = _fpsHistory.Values
				.SelectMany(x => x)
				.DefaultIfEmpty((0f, 0f, 0f))
				.Max(x => x.Item3);

			var his = _fpsHistory.ToArray();
			Array.Sort(his, (x, y) => string.Compare(x.Key, y.Key, StringComparison.Ordinal));

			for (var i = 0; i < his.Length; i++) {
				var history = his[i].Value;
				PerformanceMonitor.DrawDataLine(
					_graphTexture, history.Select(x => x.Item2).ToList(),
					maxValue,
					PerformanceMonitor.HSLToRGB(
						i / (float)his.Length * 360f, 0.7f, 0.5f
					), true
				);
			}

			// Apply the changes to the texture
			_graphTexture.Apply();
			_graphImage?.MarkDirtyRepaint();
		}

		public class LineRef {
			public  string     Name;
			private (int, int) _index   = (-1, -1);
			private float      _current = -1;
			private float      _min     = -1;
			private float      _avg     = -1;
			private float      _max     = -1;

			internal void UpdateColor(int index, int total) {
				if (index == _index.Item1 && total == _index.Item2) return;
				_index = (index, total);
				if (_colorBox != null)
					_colorBox.style.backgroundColor = new StyleColor(
						PerformanceMonitor.HSLToRGB(
							index / (float)total * 360f, 0.7f, 0.5f
						)
					);
			}

			internal void UpdateCurrent(float current) {
				if (Mathf.Approximately(_current, current)) return;
				_current = current;
				if (_crtLabel != null)
					_crtLabel.text = Mathf.Abs(_current).ToString("F2");
			}

			internal void UpdateMin(float min) {
				if (Mathf.Approximately(_min, min)) return;
				_min = min;
				if (_minLabel != null)
					_minLabel.text = Mathf.Abs(_min).ToString("F2");
			}

			internal void UpdateAvg(float avg) {
				if (Mathf.Approximately(_avg, avg)) return;
				_avg = avg;
				if (_avgLabel != null)
					_avgLabel.text = Mathf.Abs(_avg).ToString("F2");
			}

			internal void UpdateMax(float max) {
				if (Mathf.Approximately(_max, max)) return;
				_max = max;
				if (_maxLabel != null)
					_maxLabel.text = _max.ToString("F2");
			}

			private readonly VisualElement _lineElement;
			private readonly Label         _crtLabel;
			private readonly Label         _minLabel;
			private readonly Label         _avgLabel;
			private readonly Label         _maxLabel;
			private readonly VisualElement _colorBox;

			internal void Remove() {
				_lineElement?.RemoveFromHierarchy();
			}

			internal LineRef(string name, VisualElement parent) {
				Name                  = name;
				_lineElement          = Performances.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("mods-perf-line.uxml").CloneTree();
				_lineElement.userData = name;
				parent.Add(_lineElement);

				var nameLabel = _lineElement.Q<Label>("name");
				_crtLabel = _lineElement.Q<Label>("crt-ms");
				_minLabel = _lineElement.Q<Label>("min-ms");
				_avgLabel = _lineElement.Q<Label>("avg-ms");
				_maxLabel = _lineElement.Q<Label>("max-ms");
				_colorBox = _lineElement.Q<VisualElement>("color");

				if (nameLabel != null)
					nameLabel.text = name;
			}
		}

		private readonly List<LineRef> _lines = new();


		private void UpdateLabels(Mod mod, Performance[] profilers) {
			if (mod == null) return;

			_foldout.text = $"{mod.GetMetadata().GetName()} - {mod.GetMetadata().GetId()}@{mod.GetMetadata().GetVersion()}";

			if (!CanUpdate()) return;

			var present = _lines.Select(l => l.Name).ToList();

			// Remove old entries
			foreach (var line in _lines.ToList().Where(line => profilers.All(p => p.GetName() != line.Name))) {
				line.Remove();
				_lines.Remove(line);
			}

			// Add new entries
			foreach (var profiler in profilers) {
				var n = profiler.GetName();
				if (present.Contains(n)) continue;
				_lines.Add(new LineRef(n, _data));
			}


			// Update existing entries (name, crt-ms, min-ms, avg-ms, max-ms, color)
			for (var i = 0; i < profilers.Length; i++) {
				var profiler = profilers[i];
				var line     = _lines.FirstOrDefault(l => l.Name == profiler.GetName());
				if (line == null) continue;
				line.UpdateColor(i, profilers.Length);
				line.UpdateCurrent((float)profiler.Duration.TotalMilliseconds);
				if (!_fpsHistory.TryGetValue(line.Name, out var history) || history.Count <= 0) continue;
				line.UpdateAvg(history.Average(x => x.Item2));
				line.UpdateMin(history.Min(x => x.Item1));
				line.UpdateMax(history.Max(x => x.Item3));
			}
		}

		internal void Dispose() {
			if (_instance == null) return;
			var parent = _instance.parent;
			parent?.Remove(_instance);
			foreach (var child in _lines.ToArray())
				child.Remove();
			_lines.Clear();
			_instance.Clear();
			_instance = null;
		}
	}

	public class PerformanceMonitor {
		private          VisualElement _graphContainer;
		private readonly Foldout       _graphFoldout;
		private          Label         _fpsLabel;
		private          Label         _memoryLabel;
		private          Label         _drawCallsLabel;
		private          Image         _graphImage;
		private          Texture2D     _graphTexture;

		private bool CanUpdate() {
			return _graphFoldout is { value: true };
		}

		internal PerformanceMonitor(VisualElement root) {
			_graphFoldout   = root.Q<Foldout>("perf-graph-foldout");
			_graphContainer = root.Q<VisualElement>("perf-graphic");
			_fpsLabel       = root.Q<Label>("fps-label");
			_memoryLabel    = root.Q<Label>("memory-label");
			_drawCallsLabel = root.Q<Label>("draw-calls-label");
			InitializeGraphElements();
		}

		internal void Dispose() {
			_graphContainer = null;
			_fpsLabel       = null;
			_memoryLabel    = null;
			_drawCallsLabel = null;

			if (_graphFoldout != null) {
				_graphFoldout.value = false;
				_graphFoldout.Clear();
			}

			if (_graphTexture) {
				UnityEngine.Object.DestroyImmediate(_graphTexture);
				_graphTexture = null;
			}

			if (_graphImage != null) {
				_graphImage.RemoveFromHierarchy();
				_graphImage = null;
			}
		}

		internal void Update() {
			_fpsCurrent.Add(Time.deltaTime);

			// Check if we need to resize the texture based on container size
			CheckAndResizeTexture();

			if ((DateTime.UtcNow - _lastLabelUpdate).TotalSeconds >= UpdateLabelInterval) {
				_lastLabelUpdate = DateTime.UtcNow;
				UpdateLabels();
			}

			if ((DateTime.UtcNow - _lastGraphicUpdate).TotalSeconds >= UpdateGraphicInterval) {
				_lastGraphicUpdate = DateTime.UtcNow;
				// Calculate FPS
				var avg = _fpsCurrent.Count > 0
					? 1f / _fpsCurrent.Average()
					: 0f;
				var min = _fpsCurrent.Count > 0
					? 1f / _fpsCurrent.Max()
					: 0f;
				var max = _fpsCurrent.Count > 0
					? 1f / _fpsCurrent.Min()
					: 0f;
				_fpsCurrent.Clear();
				RecordHistory(min, avg, max);
				UpdateGraph();
			}
		}

		private void CheckAndResizeTexture() {
			if (!_graphTexture || _graphContainer == null || !CanUpdate()) return;

			// Get current container dimensions
			var containerWidth  = (int)_graphContainer.layout.width;
			var containerHeight = (int)_graphContainer.layout.height;

			// Only resize if dimensions are valid and different from current texture
			if (containerWidth      <= 0 || containerHeight                   <= 0) return;
			if (_graphTexture.width == containerWidth && _graphTexture.height == containerHeight) return;

			// Recreate texture with new dimensions
			if (_graphTexture) {
				UnityEngine.Object.DestroyImmediate(_graphTexture);
			}

			_graphTexture = new Texture2D(containerWidth, containerHeight, TextureFormat.RGBA32, false) {
				filterMode = FilterMode.Point
			};

			// Update the image with the new texture
			if (_graphImage != null) {
				_graphImage.image = _graphTexture;
			}

			// Redraw everything
			UpdateGraph();
		}

		private          DateTime                    _lastGraphicUpdate = DateTime.UtcNow;
		private          DateTime                    _lastLabelUpdate   = DateTime.UtcNow;
		private readonly List<float>                 _fpsCurrent        = new();
		private readonly List<(float, float, float)> _fpsHistory        = new();
		private readonly List<float>                 _memoryHistory     = new();
		private readonly List<float>                 _drawCallsHistory  = new();

		private const int   MaxHistoryPoints      = 256;
		private const float UpdateGraphicInterval = 1f / MaxHistoryPoints * 10;
		private const float UpdateLabelInterval   = 0.5f;

		private void InitializeGraphElements() {
			if (_graphContainer == null) return;

			_graphContainer.Clear();

			// Get initial container dimensions
			var containerWidth  = Mathf.Max(1, (int)_graphContainer.layout.width);
			var containerHeight = Mathf.Max(1, (int)_graphContainer.layout.height);

			// If the layout isn't ready yet, use some reasonable defaults
			if (containerWidth <= 1 || containerHeight <= 1) {
				containerWidth  = 512;
				containerHeight = 256;
			}

			// Create the texture for the graph
			_graphTexture = new Texture2D(containerWidth, containerHeight, TextureFormat.RGBA32, false) {
				filterMode = FilterMode.Point
			};

			// Create the image element that will display the texture
			_graphImage = new Image {
				image = _graphTexture,
				style = {
					width  = new StyleLength(Length.Percent(100)),
					height = new StyleLength(Length.Percent(100))
				}
			};

			_graphContainer.Add(_graphImage);

			// Add legend
			var legend = new VisualElement {
				style = {
					position        = Position.Absolute,
					top             = 5,
					right           = 5,
					backgroundColor = new StyleColor(new Color(0, 0, 0, 0.5f)),
					paddingBottom   = 5,
					paddingLeft     = 5,
					paddingRight    = 5,
					paddingTop      = 5,
					flexDirection   = FlexDirection.Column
				}
			};

			// FPS legend entry (Average)
			var fpsRow = new VisualElement {
				style = {
					flexDirection = FlexDirection.Row,
					alignItems    = Align.Center,
					marginBottom  = 2
				}
			};

			var fpsColor = new VisualElement {
				style = {
					width           = 12,
					height          = 12,
					backgroundColor = new StyleColor(new Color(0, 1, 0, 0.7f)),
					marginRight     = 5
				}
			};

			var fpsText = new Label("Average Ms");

			fpsRow.Add(fpsColor);
			fpsRow.Add(fpsText);

			// Min and Max FPS legend entries
			var minFpsRow = new VisualElement {
				style = {
					flexDirection = FlexDirection.Row,
					alignItems    = Align.Center,
					marginBottom  = 2
				}
			};
			var minFpsColor = new VisualElement {
				style = {
					width           = 12,
					height          = 12,
					backgroundColor = new StyleColor(new Color(0, 1, 1, 0.7f)),
					marginRight     = 5
				}
			};
			var minFpsText = new Label("Min Ms");
			minFpsRow.Add(minFpsColor);
			minFpsRow.Add(minFpsText);
			var maxFpsRow = new VisualElement {
				style = {
					flexDirection = FlexDirection.Row,
					alignItems    = Align.Center,
					marginBottom  = 2
				}
			};
			var maxFpsColor = new VisualElement {
				style = {
					width           = 12,
					height          = 12,
					backgroundColor = new StyleColor(new Color(1, 1, 0, 0.7f)),
					marginRight     = 5
				}
			};
			var maxFpsText = new Label("Max Ms");
			maxFpsRow.Add(maxFpsColor);
			maxFpsRow.Add(maxFpsText);

			// Memory legend entry
			var memRow = new VisualElement {
				style = {
					flexDirection = FlexDirection.Row,
					alignItems    = Align.Center,
					marginBottom  = 2
				}
			};

			var memColor = new VisualElement {
				style = {
					width           = 12,
					height          = 12,
					backgroundColor = new StyleColor(new Color(0, 0.5f, 1, 0.7f)),
					marginRight     = 5
				}
			};

			var memText = new Label("Memory");

			memRow.Add(memColor);
			memRow.Add(memText);

			// Draw Calls legend entry
			var drawCallsRow = new VisualElement {
				style = {
					flexDirection = FlexDirection.Row,
					alignItems    = Align.Center
				}
			};

			var drawCallsColor = new VisualElement {
				style = {
					width           = 12,
					height          = 12,
					backgroundColor = new StyleColor(new Color(1, 0.5f, 0, 0.7f)),
					marginRight     = 5
				}
			};

			var drawCallsText = new Label("Draw Calls");

			drawCallsRow.Add(drawCallsColor);
			drawCallsRow.Add(drawCallsText);

			legend.Add(fpsRow);
			legend.Add(maxFpsRow);
			legend.Add(minFpsRow);
			legend.Add(memRow);
			legend.Add(drawCallsRow);

			_graphContainer.Add(legend);

			// Initial draw of the graph
			Fill(_graphTexture);
		}

		private (float, float, float) CurrentFps
			=> _fpsHistory.Count == 0 ? (0, 0, 0) : _fpsHistory.Last();

		private void UpdateLabels() {
			if (_fpsLabel != null) {
				var crt = CurrentFps;
				_fpsLabel.text = $"FPS: {crt.Item2:F1} (Min: {crt.Item1:F1}, Max: {crt.Item3:F1})";
			}


			if (_memoryLabel != null) {
				var memoryUsageMb = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
				_memoryLabel.text = $"Memory: {memoryUsageMb:F1} MB";
			}

			if (_drawCallsLabel == null) return;
			var drawCalls = UnityStats.drawCalls;
			_drawCallsLabel.text = $"Draw Calls: {drawCalls}";
		}

		private void RecordHistory(float min, float avg, float max) {
			// Add current values to history
			_fpsHistory.Add((min, avg, max));

			var memoryUsageMb = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
			_memoryHistory.Add(memoryUsageMb);

			var drawCalls = UnityStats.drawCalls;
			_drawCallsHistory.Add(drawCalls);

			// Limit history size
			if (_fpsHistory.Count > MaxHistoryPoints)
				_fpsHistory.RemoveAt(0);

			if (_memoryHistory.Count > MaxHistoryPoints)
				_memoryHistory.RemoveAt(0);

			if (_drawCallsHistory.Count > MaxHistoryPoints)
				_drawCallsHistory.RemoveAt(0);
		}

		public static void Fill(Texture2D texture) {
			var clearColor = new Color(0.15f, 0.15f, 0.15f, 1.0f);
			var pixels     = new Color[texture.width * texture.height];
			for (int i = 0; i < pixels.Length; i++) {
				pixels[i] = clearColor;
			}

			texture.SetPixels(pixels);
			texture.Apply();
		}

		private void UpdateGraph() {
			if (!_graphTexture || _graphContainer == null || !CanUpdate()) return;
			// Clear the texture with a dark background
			Fill(_graphTexture);

			// Find max values for scaling
			var maxFps       = _fpsHistory.Count       > 0 ? Mathf.Max(_fpsHistory.Max(x => x.Item3), 60) : 60;
			var maxMemory    = _memoryHistory.Count    > 0 ? Mathf.Max(_memoryHistory.Max(), 100) : 100;
			var maxDrawCalls = _drawCallsHistory.Count > 0 ? Mathf.Max(_drawCallsHistory.Max(), 100) : 100;

			// Draw grid lines
			DrawGrid(_graphTexture);

			// Draw the data lines with reversed Y (higher values at the bottom)
			DrawDataLine(
				_graphTexture, _fpsHistory.Select(x => x.Item1).ToList(),
				maxFps, new Color(0, 1, 1, 0.7f), true
			);
			DrawDataLine(
				_graphTexture, _fpsHistory.Select(x => x.Item3).ToList(),
				maxFps, new Color(1, 1, 0, 0.7f), true
			);
			DrawDataLine(
				_graphTexture, _fpsHistory.Select(x => x.Item2).ToList(),
				maxFps, new Color(0, 1, 0, 0.7f), true
			);

			// Draw memory and draw calls history
			DrawDataLine(
				_graphTexture, _memoryHistory,
				maxMemory, new Color(0, 0.5f, 1, 0.7f),
				true
			);
			DrawDataLine(
				_graphTexture, _drawCallsHistory,
				maxDrawCalls, new Color(1, 0.5f, 0, 0.7f),
				true
			);

			// Apply the changes to the texture
			_graphTexture.Apply();
			_graphImage?.MarkDirtyRepaint();
		}

		public static void DrawGrid(Texture2D texture) {
			var gridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
			var width     = texture.width;
			var height    = texture.height;

			// Draw horizontal grid lines
			for (int y = 0; y < height; y += height / 4)
			for (int x = 0; x < width; x++)
				texture.SetPixel(x, y, gridColor);

			// Draw vertical grid lines
			for (int x = 0; x < width; x += width / 10)
			for (int y = 0; y < height; y++)
				texture.SetPixel(x, y, gridColor);
		}

		public static void DrawDataLine(Texture2D texture, List<float> data, float maxValue, Color lineColor, bool reverseY = false) {
			if (data.Count < 2) return;
			var width  = texture.width;
			var height = texture.height;

			var pointWidth = width / (float)MaxHistoryPoints;
			var lastIndex  = data.Count - 1;

			for (int i = 0; i < data.Count - 1; i++) {
				var x1 = (int)((i       / (float)lastIndex) * (width - 1));
				var x2 = (int)(((i + 1) / (float)lastIndex) * (width - 1));

				// Calculate y coordinates based on reverseY flag
				int y1, y2;
				if (reverseY) {
					// Direct mapping - higher values are at the bottom
					y1 = (int)((data[i]     / maxValue) * (height - 1));
					y2 = (int)((data[i + 1] / maxValue) * (height - 1));
				} else {
					// Inverted mapping - higher values are at the top (original behavior)
					y1 = (int)((1 - (data[i]     / maxValue)) * (height - 1));
					y2 = (int)((1 - (data[i + 1] / maxValue)) * (height - 1));
				}

				DrawLine(texture, x1, y1, x2, y2, lineColor);
			}
		}

		public static Color HSLToRGB(float h, float s, float l) {
			float c = (1f - Mathf.Abs(2f * l - 1f)) * s;
			float x = c                             * (1f - Mathf.Abs((h / 60f) % 2 - 1f));
			float m = l - c / 2f;

			float r1 = 0, g1 = 0, b1 = 0;

			if (h < 60) {
				r1 = c;
				g1 = x;
				b1 = 0;
			} else if (h < 120) {
				r1 = x;
				g1 = c;
				b1 = 0;
			} else if (h < 180) {
				r1 = 0;
				g1 = c;
				b1 = x;
			} else if (h < 240) {
				r1 = 0;
				g1 = x;
				b1 = c;
			} else if (h < 300) {
				r1 = x;
				g1 = 0;
				b1 = c;
			} else {
				r1 = c;
				g1 = 0;
				b1 = x;
			}

			return new Color(r1 + m, g1 + m, b1 + m);
		}

		public static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color) {
			int dx  = Mathf.Abs(x1 - x0);
			int dy  = Mathf.Abs(y1 - y0);
			int sx  = x0 < x1 ? 1 : -1;
			int sy  = y0 < y1 ? 1 : -1;
			int err = dx - dy;

			while (true) {
				// Set the pixel at the current coordinates
				if (x0 >= 0 && x0 < texture.width && y0 >= 0 && y0 < texture.height)
					texture.SetPixel(x0, y0, color);

				// Check if we've reached the end point
				if (x0 == x1 && y0 == y1) break;

				int e2 = 2 * err;
				if (e2 > -dy) {
					err -= dy;
					x0  += sx;
				}

				if (e2 < dx) {
					err += dx;
					y0  += sy;
				}
			}
		}
	}
}

#endif