#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Panels;
using Nox.CCK.Network;
using Nox.Players;
using Nox.Sessions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.session {
	public class SessionsPanel : IEditorPanelBuilder, IDisposable {
		#region Interface Implementation

		public string GetId()
			=> "sessions";

		public string GetName()
			=> "Sessions/Manager";

		public string GetTitle()
			=> LanguageManager.Get("session.manager.title");

		public bool IsHidden()
			=> false;

		public VisualElement[] GetHeaders() {
			var refreshButton = new Button {
				text = LanguageManager.Get("session.manager.refresh")
			};
			refreshButton.AddToClassList("nox-transparent");
			refreshButton.clicked += OnRefreshClicked;

			return new VisualElement[] { refreshButton };
		}

		#endregion

		#region Private Fields

		private readonly VisualElement _root = new();

		// Session Overview
		private Label   _sessionCountLabel;
		private Label   _currentSessionLabel;
		private HelpBox _statusHelpBox;

		// Session List
		private          MultiColumnListView    _sessionsListView;
		private readonly List<SessionViewModel> _sessionItems = new();
		private          ISession               _selectedSession;

		// Players List  
		private          MultiColumnListView   _playersListView;
		private readonly List<PlayerViewModel> _playerItems = new();
		private          IPlayer               _selectedPlayer;

		// Player Properties
		private          Foldout                 _playerPropertiesFoldout;
		private          VisualElement           _playerInfoContainer;
		private          MultiColumnListView     _propertiesListView;
		private readonly List<PropertyViewModel> _propertyItems = new();

		// Player Parts
		private          MultiColumnListView _partsListView;
		private readonly List<PartViewModel> _partItems = new();

		// Action Buttons
		private Button _createSessionButton;
		private Button _disposeAllButton;

		private readonly Dictionary<ISession, ISessionEvents> _sessionEventsSubscriptions = new();

		#endregion

		#region View Models

		private class SessionViewModel {
			public readonly  ISession Session;
			private readonly ISession _currentSession;

			public SessionViewModel(ISession session, ISession currentSession) {
				Session         = session;
				_currentSession = currentSession;
			}

			public int GetId()
				=> Session?.GetId() ?? -1;

			public string GetIdDisplay()
				=> $"#{GetId()}";

			public bool IsCurrent()
				=> Session == _currentSession;

			public string GetStatusDisplay()
				=> IsCurrent() ? "Current" : "Inactive";

			public int GetPlayerCount() {
				var adapter = Session?.GetAdapter();
				return adapter?.GetPlayers()?.Length ?? 0;
			}

			public string GetPlayerCountDisplay()
				=> $"{GetPlayerCount()} player(s)";
		}

		private class PlayerViewModel {
			public readonly IPlayer Player;

			public PlayerViewModel(IPlayer player)
				=> Player = player;

			public ushort GetId()
				=> Player?.GetId() ?? ushort.MaxValue;

			public string GetIdDisplay()
				=> $"#{GetId()}";

			public string GetDisplayName()
				=> Player?.GetDisplay() ?? "Unknown";

			public bool IsMaster()
				=> Player?.IsMaster() ?? false;

			public bool IsLocal()
				=> Player?.IsLocal() ?? false;

			public string GetRoleDisplay() {
				var roles = new List<string>();
				if (IsLocal()) roles.Add("Local");
				if (IsMaster()) roles.Add("Master");
				return roles.Count > 0 ? string.Join(" | ", roles) : "Participant";
			}
		}

		private class PropertyViewModel {
			public string Key   { get; }
			public string Value { get; }
			public string Type  { get; }
			public string Hash  { get; }

			public PropertyViewModel(string key, string value, string type = "String") {
				Key   = key;
				Value = value;
				Type  = type;
				Hash  = key?.Hash().ToString() ?? "0";
			}
		}

		private class PartViewModel {
			public readonly Nox.Entities.IPart Part;

			public PartViewModel(Nox.Entities.IPart part)
				=> Part = part;

			public ushort GetId()
				=> Part?.GetId() ?? ushort.MaxValue;

			public string GetIdDisplay()
				=> $"#{GetId()}";

			public string GetPositionDisplay() {
				if (Part == null) return "N/A";
				if (Part.TryGetPosition(out var position))
					return $"({position.x:F2}, {position.y:F2}, {position.z:F2})";
				return "N/A";
			}

			public string GetRotationDisplay() {
				if (Part == null) return "N/A";
				if (Part.TryGetRotation(out var rotation)) {
					var euler = rotation.eulerAngles;
					return $"({euler.x:F1}°, {euler.y:F1}°, {euler.z:F1}°)";
				}

				return "N/A";
			}

			public string GetScaleDisplay() {
				if (Part == null) return "N/A";
				if (Part.TryGetScale(out var scale))
					return $"({scale.x:F2}, {scale.y:F2}, {scale.z:F2})";
				return "N/A";
			}
		}

		#endregion

		#region Lifecycle & Updates

		internal void Update() {
			RefreshAll();
		}

		private void OnRefreshClicked() {
			RefreshAll();
		}

		private void RefreshAll() {
			if (Main.Instance == null) return;

			var sessions       = Main.Instance.GetAllSessions();
			var currentSession = Main.Instance.GetCurrent();

			UpdateOverview(sessions, currentSession);
			UpdateSessionsList(sessions, currentSession);
			UpdatePlayersList();
			UpdatePlayerProperties();
			UpdateActionsState(sessions);
		}

		private void UpdateOverview(ISession[] sessions, ISession currentSession) {
			// Update session count
			if (_sessionCountLabel != null) {
				_sessionCountLabel.text = $"{sessions.Length}";
			}

			// Update current session
			if (_currentSessionLabel != null) {
				_currentSessionLabel.text = currentSession != null ? $"#{currentSession.GetId()}" : "None";
			}

			// Update status
			if (_statusHelpBox != null) {
				if (sessions.Length == 0) {
					_statusHelpBox.text          = "No sessions active. Create or join a session to begin.";
					_statusHelpBox.messageType   = HelpBoxMessageType.Info;
					_statusHelpBox.style.display = DisplayStyle.Flex;
				} else {
					_statusHelpBox.style.display = DisplayStyle.None;
				}
			}
		}

		private void UpdateSessionsList(ISession[] sessions, ISession currentSession) {
			if (_sessionsListView == null) return;

			// Update view models
			_sessionItems.Clear();
			foreach (var session in sessions.OrderBy(s => s.GetId())) {
				_sessionItems.Add(new SessionViewModel(session, currentSession));
				SubscribeToSessionEvents(session);
			}

			// Force complete rebuild
			_sessionsListView.itemsSource = null;
			_sessionsListView.itemsSource = _sessionItems;
			_sessionsListView.Rebuild();

			// Auto-select current or first session
			if (_selectedSession == null) {
				_selectedSession = currentSession ?? sessions.FirstOrDefault();
			}

			// Highlight selected session
			if (_selectedSession != null) {
				var index = _sessionItems.FindIndex(s => s.Session == _selectedSession);
				if (index >= 0) {
					_sessionsListView.selectedIndex = index;
				}
			}
		}

		private void UpdatePlayersList() {
			if (_playersListView == null) return;

			if (_selectedSession == null) {
				_playerItems.Clear();
				_playersListView.itemsSource = null;
				_playersListView.itemsSource = _playerItems;
				_playersListView.Rebuild();
				return;
			}

			var adapter = _selectedSession.GetAdapter();
			if (adapter == null) {
				_playerItems.Clear();
				_playersListView.itemsSource = null;
				_playersListView.itemsSource = _playerItems;
				_playersListView.Rebuild();
				return;
			}

			// Update view models
			_playerItems.Clear();
			var players = adapter.GetPlayers();
			foreach (var player in players.OrderBy(p => p.GetId())) {
				_playerItems.Add(new PlayerViewModel(player));
			}

			// Force complete rebuild
			_playersListView.itemsSource = null;
			_playersListView.itemsSource = _playerItems;
			_playersListView.Rebuild();
		}

		private void UpdateActionsState(ISession[] sessions) {
			if (_disposeAllButton != null) {
				_disposeAllButton.SetEnabled(sessions.Length > 0);
			}
		}

		#endregion

		#region UI Creation

		public VisualElement Make(Dictionary<string, object> data) {
			_root.Clear();

			// Create main scroll view
			var scrollView = new ScrollView(ScrollViewMode.Vertical) {
				style = {
					flexGrow = 1
				}
			};

			// Create container inside scroll view
			var container = new VisualElement {
				style = {
					paddingBottom = 10,
					paddingLeft   = 10,
					paddingRight  = 10,
					paddingTop    = 10
				}
			};

			scrollView.Add(container);
			_root.Add(scrollView);

			// Add sections to container
			CreateOverviewSection(container);
			CreateSessionsSection(container);
			CreatePlayersSection(container);
			CreatePlayerPropertiesSection(container);
			CreatePlayerPartsSection(container);
			CreateActionsSection(container);

			RefreshAll();
			return _root;
		}

		private void CreateOverviewSection(VisualElement parent) {
			var groupBox = new GroupBox("Session Overview") {
				style = {
					marginBottom  = 10,
					paddingBottom = 10,
					paddingLeft   = 10,
					paddingRight  = 10,
					paddingTop    = 10
				}
			};

			var box = new Box {
				style = {
					paddingBottom   = 10,
					paddingLeft     = 10,
					paddingRight    = 10,
					paddingTop      = 10,
					backgroundColor = new UnityEngine.Color(0.2f, 0.2f, 0.2f, 0.3f)
				}
			};

			// Session count
			var countRow = new VisualElement {
				style = { flexDirection = FlexDirection.Row, marginBottom = 5 }
			};
			countRow.Add(
				new Label("Total Sessions:") {
					style = { width = 150, unityFontStyleAndWeight = UnityEngine.FontStyle.Bold }
				}
			);
			_sessionCountLabel = new Label("0");
			countRow.Add(_sessionCountLabel);
			box.Add(countRow);

			// Current session
			var currentRow = new VisualElement {
				style = { flexDirection = FlexDirection.Row, marginBottom = 5 }
			};
			currentRow.Add(
				new Label("Current Session:") {
					style = { width = 150, unityFontStyleAndWeight = UnityEngine.FontStyle.Bold }
				}
			);
			_currentSessionLabel = new Label("None");
			currentRow.Add(_currentSessionLabel);
			box.Add(currentRow);

			// Status help box
			_statusHelpBox = new HelpBox("Loading...", HelpBoxMessageType.Info) {
				style = { marginTop = 8 }
			};
			box.Add(_statusHelpBox);

			groupBox.Add(box);
			parent.Add(groupBox);
		}

		private void CreateSessionsSection(VisualElement parent) {
			var groupBox = new GroupBox("Sessions") {
				style = {
					marginBottom = 10,
					minHeight    = 200,
					maxHeight    = 400
				}
			};

			var foldout = new Foldout {
				text  = "Sessions List",
				value = true,
				style = { marginBottom = 5 }
			};

			// Create sessions list view
			_sessionsListView = new MultiColumnListView {
				showBorder                    = true,
				showAlternatingRowBackgrounds = AlternatingRowBackground.All,
				fixedItemHeight               = 24,
				selectionType                 = SelectionType.Single,
				style = {
					flexGrow  = 1,
					minHeight = 150
				}
			};

			// Selection changed
			_sessionsListView.selectionChanged += OnSessionSelectionChanged;

			// Add to foldout FIRST
			foldout.Add(_sessionsListView);

			// Setup columns AFTER adding to hierarchy
			SetupSessionsListViewColumns();

			// Set items source AFTER columns are configured
			_sessionsListView.itemsSource = _sessionItems;

			groupBox.Add(foldout);
			parent.Add(groupBox);
		}

		private void SetupSessionsListViewColumns() {
			// Clear existing columns
			_sessionsListView.columns.Clear();

			// ID Column
			var idColumn = new Column {
				title    = "ID",
				width    = 60,
				minWidth = 60,
				makeCell = () => new Label {
					style = {
						unityTextAlign = UnityEngine.TextAnchor.MiddleCenter
					}
				},
				bindCell = (element, index) => {
					if (element is not Label label) return;
					if (index < 0 || index >= _sessionItems.Count) {
						label.text = "ERROR";
						return;
					}

					var vm = _sessionItems[index];
					label.text = vm.GetIdDisplay();
				}
			};
			_sessionsListView.columns.Add(idColumn);

			// Status Column
			var statusColumn = new Column {
				title    = "Status",
				width    = 100,
				minWidth = 80,
				makeCell = () => new Label {
					style = {
						unityTextAlign = UnityEngine.TextAnchor.MiddleLeft
					}
				},
				bindCell = (element, index) => {
					if (!(element is Label label)) return;
					if (index < 0 || index >= _sessionItems.Count) {
						label.text = "ERROR";
						return;
					}

					var vm = _sessionItems[index];
					label.text = vm.GetStatusDisplay();
					label.style.color = vm.IsCurrent()
						? new UnityEngine.Color(0.3f, 0.8f, 0.3f)
						: UnityEngine.Color.white;
				}
			};
			_sessionsListView.columns.Add(statusColumn);

			// Players Column
			var playersColumn = new Column {
				title       = "Players",
				width       = 100,
				minWidth    = 80,
				stretchable = true,
				makeCell = () => new Label {
					style = {
						unityTextAlign = UnityEngine.TextAnchor.MiddleLeft
					}
				},
				bindCell = (element, index) => {
					if (element is not Label label) return;
					if (index < 0 || index >= _sessionItems.Count) {
						label.text = "ERROR";
						return;
					}

					var vm = _sessionItems[index];
					label.text = vm.GetPlayerCountDisplay();
				}
			};
			_sessionsListView.columns.Add(playersColumn);

			// Actions Column
			var actionsColumn = new Column {
				title    = "Actions",
				width    = 120,
				minWidth = 100,
				makeCell = CreateSessionActionButtons,
				bindCell = (element, index) => {
					if (element == null) return;
					if (index < 0 || index >= _sessionItems.Count) return;
					var vm = _sessionItems[index];
					BindSessionActionButtons(element, vm);
				}
			};
			_sessionsListView.columns.Add(actionsColumn);
		}

		private static VisualElement CreateSessionActionButtons() {
			var container = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.Center } };

			var switchButton = new Button {
				text  = "Switch",
				style = { fontSize = 10, marginRight = 2, height = 20, minWidth = 50 },
				name  = "switchButton"
			};
			container.Add(switchButton);

			var disposeButton = new Button {
				text  = "Dispose",
				style = { fontSize = 10, height = 20, minWidth = 50 },
				name  = "disposeButton"
			};
			container.Add(disposeButton);

			return container;
		}

		private void BindSessionActionButtons(VisualElement container, SessionViewModel vm) {
			var switchButton  = container.Q<Button>("switchButton");
			var disposeButton = container.Q<Button>("disposeButton");

			if (switchButton != null) {
				switchButton.SetEnabled(!vm.IsCurrent());
				switchButton.clicked -= null;
				switchButton.clicked += () => SwitchToSession(vm.Session).Forget();
			}

			if (disposeButton != null) {
				disposeButton.clicked -= null;
				disposeButton.clicked += () => DisposeSession(vm.Session).Forget();
			}
		}

		private void OnSessionSelectionChanged(IEnumerable<object> selectedItems) {
			var selected = selectedItems.FirstOrDefault();
			if (selected is SessionViewModel vm) {
				_selectedSession = vm.Session;
				UpdatePlayersList();
			}
		}

		private void CreatePlayersSection(VisualElement parent) {
			var groupBox = new GroupBox("Players in Selected Session") {
				style = {
					marginBottom = 10,
					minHeight    = 200,
					maxHeight    = 400
				}
			};

			var foldout = new Foldout {
				text  = "Players List",
				value = true,
				style = { marginBottom = 5 }
			};

			// Create players list view
			_playersListView = new MultiColumnListView {
				showBorder                    = true,
				showAlternatingRowBackgrounds = AlternatingRowBackground.All,
				fixedItemHeight               = 24,
				selectionType                 = SelectionType.Single,
				style = {
					flexGrow  = 1,
					minHeight = 150
				}
			};

			// Selection changed
			_playersListView.selectionChanged += OnPlayerSelectionChanged;

			// Add to foldout FIRST
			foldout.Add(_playersListView);

			// Setup columns AFTER adding to hierarchy
			SetupPlayersListViewColumns();

			// Set items source AFTER columns are configured
			_playersListView.itemsSource = _playerItems;

			groupBox.Add(foldout);
			parent.Add(groupBox);
		}

		private void SetupPlayersListViewColumns() {
			// Clear existing columns
			_playersListView.columns.Clear();

			// ID Column
			var idColumn = new Column {
				title    = "ID",
				width    = 60,
				minWidth = 60,
				makeCell = () => new Label {
					style = {
						unityTextAlign = UnityEngine.TextAnchor.MiddleCenter
					}
				},
				bindCell = (element, index) => {
					if (!(element is Label label)) return;
					if (index < 0 || index >= _playerItems.Count) {
						label.text = "ERROR";
						return;
					}

					var vm = _playerItems[index];
					label.text = vm.GetIdDisplay();
				}
			};
			_playersListView.columns.Add(idColumn);

			// Name Column
			var nameColumn = new Column {
				title       = "Display Name",
				width       = 150,
				minWidth    = 100,
				stretchable = true,
				makeCell = () => new Label {
					style = {
						unityTextAlign = UnityEngine.TextAnchor.MiddleLeft
					}
				},
				bindCell = (element, index) => {
					if (!(element is Label label)) return;
					if (index < 0 || index >= _playerItems.Count) {
						label.text = "ERROR";
						return;
					}

					var vm = _playerItems[index];
					label.text = vm.GetDisplayName();
				}
			};
			_playersListView.columns.Add(nameColumn);

			// Role Column
			var roleColumn = new Column {
				title    = "Role",
				width    = 120,
				minWidth = 100,
				makeCell = () => new Label {
					style = {
						unityTextAlign = UnityEngine.TextAnchor.MiddleLeft
					}
				},
				bindCell = (element, index) => {
					if (element is not Label label) return;
					if (index < 0 || index >= _playerItems.Count) {
						label.text = "ERROR";
						return;
					}

					var vm = _playerItems[index];
					label.text = vm.GetRoleDisplay();
					label.style.color = vm.IsLocal()
						? new UnityEngine.Color(0.3f, 0.8f, 0.3f)
						: UnityEngine.Color.white;
				}
			};
			_playersListView.columns.Add(roleColumn);

			// Actions Column
			var actionsColumn = new Column {
				title    = "Actions",
				width    = 100,
				minWidth = 80,
				makeCell = CreatePlayerActionButtons,
				bindCell = (element, index) => {
					if (element == null) return;
					if (index < 0 || index >= _playerItems.Count) return;
					var vm = _playerItems[index];
					BindPlayerActionButtons(element, vm);
				}
			};
			_playersListView.columns.Add(actionsColumn);
		}

		private static VisualElement CreatePlayerActionButtons() {
			var container = new VisualElement {
				style = {
					flexDirection  = FlexDirection.Row,
					justifyContent = Justify.Center
				}
			};

			var kickButton = new Button {
				text  = "Kick",
				style = { fontSize = 10, height = 20, minWidth = 60 },
				name  = "kickButton"
			};
			container.Add(kickButton);

			return container;
		}

		private void BindPlayerActionButtons(VisualElement container, PlayerViewModel vm) {
			var kickButton = container.Q<Button>("kickButton");

			if (kickButton != null) {
				// Can only kick remote players if you're master
				kickButton.SetEnabled(!vm.IsLocal() && _selectedSession != null);
				kickButton.clicked -= null;
				kickButton.clicked += () => KickPlayer(vm.Player).Forget();
			}
		}

		private void OnPlayerSelectionChanged(IEnumerable<object> selectedItems) {
			var selected = selectedItems.FirstOrDefault();
			if (selected is PlayerViewModel vm) {
				_selectedPlayer = vm.Player;
				UpdatePlayerProperties();
			}
		}

		private void CreatePlayerPropertiesSection(VisualElement parent) {
			var groupBox = new GroupBox("Selected Player Properties") {
				style = {
					marginBottom = 10,
					minHeight    = 250,
					maxHeight    = 500
				}
			};

			_playerPropertiesFoldout = new Foldout {
				text  = "Details",
				value = true,
				style = { marginBottom = 5 }
			};

			// Player info container (for basic info labels)
			_playerInfoContainer = new VisualElement {
				style = {
					paddingLeft   = 5,
					paddingRight  = 5,
					paddingTop    = 5,
					paddingBottom = 5,
					marginBottom  = 5
				}
			};

			var noSelectionLabel = new Label("No player selected") {
				style = {
					unityTextAlign          = UnityEngine.TextAnchor.MiddleCenter,
					color                   = new UnityEngine.Color(0.7f, 0.7f, 0.7f),
					fontSize                = 12,
					unityFontStyleAndWeight = UnityEngine.FontStyle.Italic,
					paddingTop              = 10,
					paddingBottom           = 10
				}
			};
			_playerInfoContainer.Add(noSelectionLabel);

			_playerPropertiesFoldout.Add(_playerInfoContainer);

			// Properties list view
			_propertiesListView = new MultiColumnListView {
				showBorder                    = true,
				showAlternatingRowBackgrounds = AlternatingRowBackground.All,
				fixedItemHeight               = 22,
				selectionType                 = SelectionType.None,
				style = {
					flexGrow    = 1,
					minHeight   = 150,
					marginLeft  = 5,
					marginRight = 5,
					display     = DisplayStyle.None // Hidden by default
				}
			};

			_playerPropertiesFoldout.Add(_propertiesListView);

			// Setup columns
			SetupPropertiesListViewColumns();

			// Set items source
			_propertiesListView.itemsSource = _propertyItems;

			groupBox.Add(_playerPropertiesFoldout);
			parent.Add(groupBox);
		}

		private void CreatePlayerPartsSection(VisualElement parent) {
			var groupBox = new GroupBox("Selected Player Parts") {
				style = {
					marginBottom = 10,
					minHeight    = 200,
					maxHeight    = 400
				}
			};

			var foldout = new Foldout {
				text  = "Parts List",
				value = true,
				style = { marginBottom = 5 }
			};

			// Create parts list view
			_partsListView = new MultiColumnListView {
				showBorder                    = true,
				showAlternatingRowBackgrounds = AlternatingRowBackground.All,
				fixedItemHeight               = 24,
				selectionType                 = SelectionType.None,
				style = {
					flexGrow  = 1,
					minHeight = 150
				}
			};

			// Add to foldout FIRST
			foldout.Add(_partsListView);

			// Setup columns AFTER adding to hierarchy
			SetupPartsListViewColumns();

			// Set items source AFTER columns are configured
			_partsListView.itemsSource = _partItems;

			groupBox.Add(foldout);
			parent.Add(groupBox);
		}

		private void SetupPartsListViewColumns() {
			// Clear existing columns
			_partsListView.columns.Clear();

			// ID Column
			var idColumn = new Column {
				title    = "Part ID",
				width    = 80,
				minWidth = 60,
				makeCell = () => new Label {
					style = {
						unityTextAlign = UnityEngine.TextAnchor.MiddleCenter
					}
				},
				bindCell = (element, index) => {
					if (element is not Label label) return;
					if (index < 0 || index >= _partItems.Count) {
						label.text = "ERROR";
						return;
					}

					var vm = _partItems[index];
					label.text = vm.GetIdDisplay();
				}
			};
			_partsListView.columns.Add(idColumn);

			// Position Column
			var positionColumn = new Column {
				title       = "Position",
				width       = 150,
				minWidth    = 120,
				stretchable = false,
				makeCell = () => new Label {
					style = {
						unityTextAlign = UnityEngine.TextAnchor.MiddleLeft,
						fontSize       = 10
					}
				},
				bindCell = (element, index) => {
					if (element is not Label label) return;
					if (index < 0 || index >= _partItems.Count) {
						label.text = "ERROR";
						return;
					}

					var vm = _partItems[index];
					label.text = vm.GetPositionDisplay();
				}
			};
			_partsListView.columns.Add(positionColumn);

			// Rotation Column
			var rotationColumn = new Column {
				title       = "Rotation",
				width       = 150,
				minWidth    = 120,
				stretchable = false,
				makeCell = () => new Label {
					style = {
						unityTextAlign = UnityEngine.TextAnchor.MiddleLeft,
						fontSize       = 10
					}
				},
				bindCell = (element, index) => {
					if (element is not Label label) return;
					if (index < 0 || index >= _partItems.Count) {
						label.text = "ERROR";
						return;
					}

					var vm = _partItems[index];
					label.text = vm.GetRotationDisplay();
				}
			};
			_partsListView.columns.Add(rotationColumn);

			// Scale Column
			var scaleColumn = new Column {
				title       = "Scale",
				width       = 120,
				minWidth    = 100,
				stretchable = true,
				makeCell = () => new Label {
					style = {
						unityTextAlign = UnityEngine.TextAnchor.MiddleLeft,
						fontSize       = 10
					}
				},
				bindCell = (element, index) => {
					if (element is not Label label) return;
					if (index < 0 || index >= _partItems.Count) {
						label.text = "ERROR";
						return;
					}

					var vm = _partItems[index];
					label.text = vm.GetScaleDisplay();
				}
			};
			_partsListView.columns.Add(scaleColumn);
		}

		private void SetupPropertiesListViewColumns() {
			// Clear existing columns
			_propertiesListView.columns.Clear();

			// Key Column
			var keyColumn = new Column {
				title       = "Property",
				width       = 150,
				minWidth    = 100,
				stretchable = false,
				makeCell = () => new Label {
					style = {
						unityTextAlign          = UnityEngine.TextAnchor.MiddleLeft,
						unityFontStyleAndWeight = UnityEngine.FontStyle.Bold,
						color                   = new UnityEngine.Color(0.8f, 0.8f, 0.8f)
					}
				},
				bindCell = (element, index) => {
					if (element is not Label label) return;
					if (index < 0 || index >= _propertyItems.Count) {
						label.text = "ERROR";
						return;
					}

					var vm = _propertyItems[index];
					label.text = vm.Key;
				}
			};
			_propertiesListView.columns.Add(keyColumn);

			// Value Column
			var valueColumn = new Column {
				title       = "Value",
				width       = 200,
				minWidth    = 100,
				stretchable = true,
				makeCell = () => new Label {
					style = {
						unityTextAlign = UnityEngine.TextAnchor.MiddleLeft
					}
				},
				bindCell = (element, index) => {
					if (element is not Label label) return;
					if (index < 0 || index >= _propertyItems.Count) {
						label.text = "ERROR";
						return;
					}

					var vm = _propertyItems[index];
					label.text = vm.Value;
				}
			};
			_propertiesListView.columns.Add(valueColumn);

			// Type Column
			var typeColumn = new Column {
				title    = "Type",
				width    = 100,
				minWidth = 80,
				makeCell = () => new Label {
					style = {
						unityTextAlign = UnityEngine.TextAnchor.MiddleCenter,
						color          = new UnityEngine.Color(0.7f, 0.7f, 0.7f),
						fontSize       = 10
					}
				},
				bindCell = (element, index) => {
					if (element is not Label label) return;
					if (index < 0 || index >= _propertyItems.Count) {
						label.text = "ERROR";
						return;
					}

					var vm = _propertyItems[index];
					label.text = vm.Type;
				}
			};
			_propertiesListView.columns.Add(typeColumn);

			// Hash Column
			var hashColumn = new Column {
				title    = "Hash",
				width    = 100,
				minWidth = 80,
				makeCell = () => new Label {
					style = {
						unityTextAlign = UnityEngine.TextAnchor.MiddleCenter,
						color          = new UnityEngine.Color(0.6f, 0.6f, 0.8f),
						fontSize       = 10
					}
				},
				bindCell = (element, index) => {
					if (element is not Label label) return;
					if (index < 0 || index >= _propertyItems.Count) {
						label.text = "ERROR";
						return;
					}

					var vm = _propertyItems[index];
					label.text = vm.Hash;
				}
			};
			_propertiesListView.columns.Add(hashColumn);
		}

		private void UpdatePlayerProperties() {
			if (_playerInfoContainer == null) return;

			_playerInfoContainer.Clear();

			if (_selectedPlayer == null) {
				var noSelectionLabel = new Label("No player selected") {
					style = {
						unityTextAlign          = UnityEngine.TextAnchor.MiddleCenter,
						color                   = new UnityEngine.Color(0.7f, 0.7f, 0.7f),
						fontSize                = 12,
						unityFontStyleAndWeight = UnityEngine.FontStyle.Italic,
						paddingTop              = 10,
						paddingBottom           = 10
					}
				};
				_playerInfoContainer.Add(noSelectionLabel);
				_propertiesListView.style.display = DisplayStyle.None;

				// Clear parts list as well
				UpdatePlayerParts();
				return;
			}

			// Player basic info box
			var infoBox = new Box {
				style = {
					marginBottom    = 10,
					paddingBottom   = 8,
					paddingLeft     = 8,
					paddingRight    = 8,
					paddingTop      = 8,
					backgroundColor = new UnityEngine.Color(0.2f, 0.2f, 0.2f, 0.3f)
				}
			};

			var titleLabel = new Label("Basic Information") {
				style = {
					fontSize                = 12,
					unityFontStyleAndWeight = UnityEngine.FontStyle.Bold,
					marginBottom            = 5
				}
			};
			infoBox.Add(titleLabel);

			// ID
			var idRow = CreatePropertyRow("ID", $"#{_selectedPlayer.GetId()}");
			infoBox.Add(idRow);

			// Display Name
			var nameRow = CreatePropertyRow("Display Name", _selectedPlayer.GetDisplay());
			infoBox.Add(nameRow);

			// Is Master
			var masterRow = CreatePropertyRow("Is Master", _selectedPlayer.IsMaster() ? "Yes" : "No");
			infoBox.Add(masterRow);

			// Is Local
			var localRow = CreatePropertyRow("Is Local", _selectedPlayer.IsLocal() ? "Yes" : "No");
			infoBox.Add(localRow);

			_playerInfoContainer.Add(infoBox);

			// Update properties list
			_propertyItems.Clear();
			var properties = _selectedPlayer.GetProperties();

			Logger.Log($"Player {_selectedPlayer.GetId()} ({_selectedPlayer.GetDisplay()}) has {properties?.Length ?? 0} properties");

			if (properties is { Length: > 0 }) {
				foreach (var prop in properties) {
					if (prop == null) {
						Logger.LogWarning("Found null property");
						continue;
					}

					var key   = prop.GetName();
					var value = prop.GetValue()?.ToString()     ?? "null";
					var type  = prop.GetValue()?.GetType().Name ?? "Unknown";

					Logger.Log($"Property: {key} = {value} ({type})");
					_propertyItems.Add(new PropertyViewModel(key, value, type));
				}

				Logger.Log($"Added {_propertyItems.Count} property items to list");

				// Show and rebuild the list view
				_propertiesListView.style.display = DisplayStyle.Flex;
				_propertiesListView.itemsSource   = null;
				_propertiesListView.itemsSource   = _propertyItems;
				_propertiesListView.Rebuild();
			} else {
				// Hide the list view if no properties
				_propertiesListView.style.display = DisplayStyle.None;

				var noPropsLabel = new Label("No Custom Properties") {
					style = {
						unityTextAlign          = UnityEngine.TextAnchor.MiddleCenter,
						color                   = new UnityEngine.Color(0.7f, 0.7f, 0.7f),
						fontSize                = 11,
						unityFontStyleAndWeight = UnityEngine.FontStyle.Italic,
						paddingTop              = 5,
						paddingBottom           = 10
					}
				};
				_playerInfoContainer.Add(noPropsLabel);
			}

			// Update parts list
			UpdatePlayerParts();
		}

		private void UpdatePlayerParts() {
			if (_partsListView == null) return;

			_partItems.Clear();

			if (_selectedPlayer == null) {
				_partsListView.itemsSource = null;
				_partsListView.itemsSource = _partItems;
				_partsListView.Rebuild();
				return;
			}

			// Get all parts from the player
			var parts = _selectedPlayer.GetParts();
			if (parts is { Length: > 0 }) {
				foreach (var part in parts.OrderBy(p => p.GetId())) {
					_partItems.Add(new PartViewModel(part));
				}
			}

			// Rebuild the list view
			_partsListView.itemsSource = null;
			_partsListView.itemsSource = _partItems;
			_partsListView.Rebuild();
		}

		private VisualElement CreatePropertyRow(string label, string value) {
			var row = new VisualElement {
				style = {
					flexDirection = FlexDirection.Row,
					marginBottom  = 3
				}
			};

			var labelElement = new Label(label + ":") {
				style = {
					width                   = 120,
					color                   = new UnityEngine.Color(0.8f, 0.8f, 0.8f),
					fontSize                = 11,
					unityFontStyleAndWeight = UnityEngine.FontStyle.Bold
				}
			};
			row.Add(labelElement);

			var valueElement = new Label(value) {
				style = {
					flexGrow = 1,
					fontSize = 11
				}
			};
			row.Add(valueElement);

			return row;
		}

		private void CreateActionsSection(VisualElement parent) {
			var groupBox = new GroupBox("Global Actions") {
				style = {
					marginBottom  = 10,
					paddingBottom = 10,
					paddingLeft   = 10,
					paddingRight  = 10,
					paddingTop    = 10
				}
			};

			var foldout = new Foldout {
				text  = "Actions",
				value = false,
				style = { marginBottom = 5 }
			};

			var buttonsContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };

			_createSessionButton = new Button(CreateSession) {
				text  = "Create New Session",
				style = { flexGrow = 1, marginRight = 5, height = 25 }
			};
			buttonsContainer.Add(_createSessionButton);

			_disposeAllButton = new Button(DisposeAllSessions) {
				text  = "Dispose All",
				style = { flexGrow = 1, height = 25 }
			};
			buttonsContainer.Add(_disposeAllButton);

			foldout.Add(buttonsContainer);
			groupBox.Add(foldout);
			parent.Add(groupBox);
		}

		#endregion

		#region Session Operations

		private void CreateSession() {
			CreateSessionAsync().Forget();
		}

		private async UniTask CreateSessionAsync() {
			if (Main.Instance == null) {
				Logger.LogError("No session manager instance available");
				EditorUtility.DisplayDialog("Error", "Session manager not available", "OK");
				return;
			}

			try {
				Logger.Log("Creating new session...");
				await UniTask.Yield();
				throw new NotImplementedException("Session creation not implemented yet");
			} catch (Exception ex) {
				Logger.LogError($"Failed to create session: {ex.Message}");
				EditorUtility.DisplayDialog("Error", $"Failed to create session: {ex.Message}", "OK");
			}
		}

		private async UniTask SwitchToSession(ISession session) {
			if (Main.Instance == null || session == null) {
				Logger.LogError("Cannot switch session: invalid state");
				return;
			}

			try {
				Logger.Log($"Switching to session {session.GetId()}");
				await Main.Instance.SetCurrent(session.GetId());
				RefreshAll();
			} catch (Exception ex) {
				Logger.LogError($"Failed to switch session: {ex.Message}");
				EditorUtility.DisplayDialog("Error", $"Failed to switch session: {ex.Message}", "OK");
			}
		}

		private async UniTask DisposeSession(ISession session) {
			if (session == null) {
				Logger.LogError("Cannot dispose null session");
				return;
			}

			try {
				Logger.Log($"Disposing session {session.GetId()}");

				if (_selectedSession == session) {
					_selectedSession = null;
				}

				UnsubscribeFromSessionEvents(session);
				await session.Dispose();

				RefreshAll();
			} catch (Exception ex) {
				Logger.LogError($"Failed to dispose session: {ex.Message}");
				EditorUtility.DisplayDialog("Error", $"Failed to dispose session: {ex.Message}", "OK");
			}
		}

		private void DisposeAllSessions() {
			DisposeAllSessionsAsync().Forget();
		}

		private async UniTask DisposeAllSessionsAsync() {
			if (Main.Instance == null) {
				Logger.LogError("No session manager instance available");
				return;
			}

			try {
				var sessions = Main.Instance.GetAllSessions();
				Logger.Log($"Disposing {sessions.Length} sessions");

				foreach (var session in sessions) {
					UnsubscribeFromSessionEvents(session);
					await session.Dispose();
				}

				_selectedSession = null;
				RefreshAll();
			} catch (Exception ex) {
				Logger.LogError($"Failed to dispose all sessions: {ex.Message}");
				EditorUtility.DisplayDialog("Error", $"Failed to dispose all sessions: {ex.Message}", "OK");
			}
		}

		private async UniTask KickPlayer(IPlayer player) {
			if (player == null || _selectedSession == null) {
				Logger.LogError("Cannot kick player: invalid state");
				return;
			}

			try {
				Logger.Log($"Kicking player {player.GetId()} from session");
				await UniTask.Yield();
				throw new NotImplementedException("Kick player not implemented yet");
			} catch (Exception ex) {
				Logger.LogError($"Failed to kick player: {ex.Message}");
				EditorUtility.DisplayDialog("Error", $"Failed to kick player: {ex.Message}", "OK");
			}
		}

		#endregion

		#region Session Events

		private void SubscribeToSessionEvents(ISession session) {
			if (session == null || _sessionEventsSubscriptions.ContainsKey(session)) return;

			if (session is not ISessionEvents events) return;

			events.OnPlayerJoinedEvent().AddListener(OnPlayerJoined);
			events.OnPlayerLeftEvent().AddListener(OnPlayerLeft);
			events.OnAuthorityTransferredEvent().AddListener(OnAuthorityTransferred);

			_sessionEventsSubscriptions[session] = events;
		}

		private void UnsubscribeFromSessionEvents(ISession session) {
			if (session == null || !_sessionEventsSubscriptions.TryGetValue(session, out var events)) return;

			if (events != null) {
				events.OnPlayerJoinedEvent().RemoveListener(OnPlayerJoined);
				events.OnPlayerLeftEvent().RemoveListener(OnPlayerLeft);
				events.OnAuthorityTransferredEvent().RemoveListener(OnAuthorityTransferred);
			}

			_sessionEventsSubscriptions.Remove(session);
		}

		private void OnPlayerJoined(IPlayer player) {
			Logger.Log($"Player {player.GetId()} joined session");
			RefreshAll();
		}

		private void OnPlayerLeft(IPlayer player) {
			Logger.Log($"Player {player.GetId()} left session");
			RefreshAll();
		}

		private void OnAuthorityTransferred(IPlayer newMaster) {
			Logger.Log($"Master changed from {newMaster.GetId()}");
			RefreshAll();
		}

		#endregion

		#region IDisposable

		public void Dispose() {
			foreach (var session in _sessionEventsSubscriptions.Keys.ToList()) {
				UnsubscribeFromSessionEvents(session);
			}

			_sessionEventsSubscriptions.Clear();
		}

		#endregion
	}
}
#endif