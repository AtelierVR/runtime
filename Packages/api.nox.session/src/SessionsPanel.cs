#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Panels;
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
			refreshButton.clicked += RefreshSessionsList;

			return new VisualElement[] { refreshButton };
		}

		#endregion

	#region Private Fields

	private readonly VisualElement       _root      = new();
	private          VisualElement       _container = new();
	private          DropdownField       _sessionDropdown;
	private          MultiColumnListView _playersListView;
	private          IntegerField        _sessionCountField;
	private          TextField           _currentSessionField;
	private          Button              _disposeAllButton;
	private          HelpBox             _statusHelpBox;
	private          ISession            _selectedSession;

	// Data tracking
	private readonly Dictionary<ISession, ISessionEvents> _sessionEventsSubscriptions = new();
	private readonly List<PlayerDisplayData>              _playerItems                = new();

		#endregion

	#region ListView Data Classes

	private class PlayerDisplayData {
		public object Player       { get; }
		public string PlayerName   { get; set; }
		public string Status       { get; set; }
		public bool   IsLocal      { get; set; }
		public bool   IsMaster     { get; set; }
		public int    Index        { get; set; }

		public PlayerDisplayData(object player, int index, object localPlayer, object masterPlayer) {
			Player  = player;
			Index   = index;
			IsLocal = player == localPlayer;
			IsMaster = player == masterPlayer;
			UpdateData();
		}

		public void UpdateData() {
			if (Player != null) {
				// Try to get identifier via reflection or ToString
				var identifier = Player.GetType().GetMethod("ToIdentifier")?.Invoke(Player, null);
				PlayerName = identifier?.ToString() ?? $"Player {Index}";
				
				var statusParts = new List<string>();
				if (IsLocal) statusParts.Add("Local");
				if (IsMaster) statusParts.Add("Master");
				Status = statusParts.Count > 0 ? string.Join(", ", statusParts) : "Connected";
			} else {
				PlayerName = $"Player {Index}";
				Status = "Unknown";
			}
		}
	}

		#endregion

		#region Update and Refresh

		internal void Update() {
			if (!Editor.HasSessionPanelOpened() || _container.childCount == 0) return;
			RefreshSessionsList();
		}

		private void RefreshSessionsList() {
			if (Main.Instance == null) return;

			var sessions       = Main.Instance.GetSessions();
			var currentSession = Main.Instance.GetCurrent();
			var sessionCount   = Main.Instance.GetSessionCount();

			UpdateSessionInfo(sessionCount, currentSession);
			UpdateListView(sessions, currentSession);
			UpdateGlobalActions(sessionCount);
		}

		private void UpdateSessionInfo(int sessionCount, ISession currentSession) {
			if (_sessionCountField != null) {
				_sessionCountField.value = sessionCount;
			}

			if (_currentSessionField != null) {
				_currentSessionField.value = currentSession != null ? $"Session #{currentSession.GetId()}" : "None";
			}

			if (_statusHelpBox != null) {
				if (sessionCount == 0) {
					_statusHelpBox.text        = "No active sessions. Sessions will appear here when created.";
					_statusHelpBox.messageType = HelpBoxMessageType.Info;
				} else {
					_statusHelpBox.text        = $"{sessionCount} active session(s). Current: {(currentSession != null ? $"#{currentSession.GetId()}" : "None")}";
					_statusHelpBox.messageType = HelpBoxMessageType.None;
				}
			}
		}

	private void UpdateListView(ISession[] sessions, ISession currentSession) {
		UpdateSessionDropdown(sessions, currentSession);
		UpdatePlayersList();
	}

	private void UpdateSessionDropdown(ISession[] sessions, ISession currentSession) {
		if (_sessionDropdown == null) return;

		var choices = new List<string>();
		if (sessions.Length == 0) {
			choices.Add("No sessions available");
		} else {
			foreach (var session in sessions.OrderBy(s => s.GetId())) {
				var displayName = $"Session #{session.GetId()}";
				if (session == currentSession) displayName += " (Current)";
				choices.Add(displayName);
				
				// Subscribe to events for real-time updates
				SubscribeToSessionEvents(session);
			}
		}

		_sessionDropdown.choices = choices;
		
		// Set selected session
		if (_selectedSession == null && currentSession != null) {
			_selectedSession = currentSession;
		}

		if (_selectedSession != null) {
			var selectedIndex = Array.IndexOf(sessions.OrderBy(s => s.GetId()).ToArray(), _selectedSession);
			if (selectedIndex >= 0 && selectedIndex < choices.Count) {
				_sessionDropdown.index = selectedIndex;
			}
		} else if (sessions.Length > 0) {
			_sessionDropdown.index = 0;
			_selectedSession = sessions.OrderBy(s => s.GetId()).First();
		}
	}

	private void UpdatePlayersList() {
		if (_playersListView == null || _selectedSession == null) {
			if (_playersListView != null) {
				_playerItems.Clear();
				_playersListView.itemsSource = _playerItems;
				_playersListView.RefreshItems();
			}
			return;
		}

		var adapter = _selectedSession.GetAdapter();
		if (adapter == null) {
			_playerItems.Clear();
			_playersListView.itemsSource = _playerItems;
			_playersListView.RefreshItems();
			return;
		}

		_playerItems.Clear();
		var playerCount = adapter.GetPlayerCount();
		var localPlayer = adapter.GetLocalPlayer();
		var masterPlayer = adapter.GetMasterPlayer();

		for (int i = 0; i < playerCount; i++) {
			var player = adapter.GetPlayer(i);
			if (player != null) {
				var playerData = new PlayerDisplayData(player, i, localPlayer, masterPlayer);
				_playerItems.Add(playerData);
			}
		}

		_playersListView.itemsSource = _playerItems;
		_playersListView.RefreshItems();
	}

		private void UpdateGlobalActions(int sessionCount) {
			if (_disposeAllButton != null) {
				_disposeAllButton.SetEnabled(sessionCount > 0);
			}
		}

		#endregion

		#region UI Creation

		public VisualElement Make(Dictionary<string, object> data) {
			_root.Clear();

			// Load UXML template
			var visualTree = Editor.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("sessions-panel.uxml");
			if (visualTree != null) {
				var template = visualTree.CloneTree();
				_root.Add(template);

				// Get references to UI elements
				GetUIReferences();

				// Setup programmatic elements
				SetupProgrammaticElements();
			} else {
				// Fallback to programmatic creation if UXML not found
				CreateLayoutProgrammatically();
			}

			RefreshSessionsList();
			return _root;
		}

	private void GetUIReferences() {
		// Get references to elements defined in UXML
		_sessionCountField   = _root.Q<IntegerField>("session-count-field");
		_currentSessionField = _root.Q<TextField>("current-session-field");
		_disposeAllButton    = _root.Q<Button>("dispose-all-button");
		_statusHelpBox       = _root.Q<HelpBox>("status-helpbox");
		_sessionDropdown     = _root.Q<DropdownField>("session-dropdown");

		// Setup events
		if (_disposeAllButton != null) {
			_disposeAllButton.clicked += DisposeAllSessions;
		}

		if (_sessionDropdown != null) {
			_sessionDropdown.RegisterValueChangedCallback(evt => OnSessionSelected(evt.newValue));
		}
	}

	private void SetupProgrammaticElements() {
		// Create and add the MultiColumnListView programmatically
		var sessionsContainer = _root.Q<VisualElement>("sessions-list-container");
		if (sessionsContainer != null) {
			CreatePlayersListView(sessionsContainer);
		}
	}

	private void CreateLayoutProgrammatically() {
		// Fallback method - same as before
		CreateSessionInfoSection();
		CreateSessionSelectionSection();
		CreateGlobalActionsSection();
		CreateStatusSection();
		CreatePlayersListView();
		_root.Add(_container);
	}

	private void CreateSessionSelectionSection() {
		var foldout = new Foldout { text = "Select Session", value = true };

		_sessionDropdown = new DropdownField("Session") {
			choices = new List<string> { "No sessions available" }
		};
		_sessionDropdown.RegisterValueChangedCallback(evt => OnSessionSelected(evt.newValue));
		foldout.Add(_sessionDropdown);

		_container.Add(foldout);
	}

		private void CreateSessionInfoSection() {
			var foldout = new Foldout { text = "Session Information", value = true };

			_sessionCountField = new IntegerField("Active Sessions") {
				isReadOnly = true
			};
			foldout.Add(_sessionCountField);

			_currentSessionField = new TextField("Current Session") {
				isReadOnly = true
			};
			foldout.Add(_currentSessionField);

			_container.Add(foldout);
		}

		private void CreateGlobalActionsSection() {
			var foldout = new Foldout { text = "Actions", value = false };

			_disposeAllButton = new Button(DisposeAllSessions) {
				text = "Dispose All Sessions"
			};
			foldout.Add(_disposeAllButton);

			_container.Add(foldout);
		}

		private void CreateStatusSection() {
			_statusHelpBox = new HelpBox("Loading...", HelpBoxMessageType.Info);
			_container.Add(_statusHelpBox);
	}

	private void CreatePlayersListView(VisualElement container = null) {
		// Create columns for players
		var columns = new Columns();
		columns.Add(new Column { title = "Player Name", width = 150, minWidth = 100 });
		columns.Add(new Column { title = "Status", width = 100, minWidth = 80 });
		columns.Add(new Column { title = "Actions", width = 150, minWidth = 120 });

		// Create ListView with columns
		_playersListView = new MultiColumnListView(columns);
		_playersListView.showBorder = true;
		_playersListView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
		_playersListView.fixedItemHeight = 24;

		// Set up column renderers
		if (_playersListView.columns != null) {
			var nameColumn = _playersListView.columns["Player Name"];
			if (nameColumn != null) {
				nameColumn.makeCell = () => new Label();
				nameColumn.bindCell = (element, index) => {
					if (element is Label label && index < _playerItems.Count) {
						label.text = _playerItems[index].PlayerName;
					}
				};
			}

			var statusColumn = _playersListView.columns["Status"];
			if (statusColumn != null) {
				statusColumn.makeCell = () => new Label();
				statusColumn.bindCell = (element, index) => {
					if (element is Label label && index < _playerItems.Count) {
						label.text = _playerItems[index].Status;
					}
				};
			}

			var actionsColumn = _playersListView.columns["Actions"];
			if (actionsColumn != null) {
				actionsColumn.makeCell = () => CreatePlayerActionButtons();
				actionsColumn.bindCell = (element, index) => {
					if (element is VisualElement actionContainer && index < _playerItems.Count) {
						BindPlayerActionButtons(actionContainer, _playerItems[index]);
					}
				};
			}
		}

		// Add to container or create foldout
		if (container != null) {
			container.Add(_playersListView);
		} else {
			var foldout = new Foldout { text = "Players", value = true };
			foldout.Add(_playersListView);
			_container.Add(foldout);
		}
	}

	private VisualElement CreatePlayerActionButtons() {
		var container = new VisualElement();
		container.style.flexDirection = FlexDirection.Row;

		var setCurrentButton = new Button { text = "Set Current" };
		setCurrentButton.name = "setCurrentButton";
		setCurrentButton.style.marginRight = 5;
		container.Add(setCurrentButton);

		var quitButton = new Button { text = "Quit" };
		quitButton.name = "quitButton";
		container.Add(quitButton);

		return container;
	}

	private void BindPlayerActionButtons(VisualElement container, PlayerDisplayData playerData) {
		var setCurrentButton = container.Q<Button>("setCurrentButton");
		var quitButton = container.Q<Button>("quitButton");

		if (setCurrentButton != null && quitButton != null) {
			// Disable "Set Current" if this player is already the local player
			// Enable "Quit" only for local player
			setCurrentButton.SetEnabled(!playerData.IsLocal);
			quitButton.SetEnabled(playerData.IsLocal);

			// Clear existing callbacks and add new ones
			setCurrentButton.clicked -= null;
			quitButton.clicked -= null;

			setCurrentButton.clicked += () => SetCurrentSession(_selectedSession).Forget();
			quitButton.clicked += () => QuitSession().Forget();
		}
	}

	private void OnSessionSelected(string sessionName) {
		if (Main.Instance == null) return;
		
		var sessions = Main.Instance.GetSessions().OrderBy(s => s.GetId()).ToArray();
		var currentSession = Main.Instance.GetCurrent();
		
		// Find the selected session by matching the dropdown text
		foreach (var session in sessions) {
			var displayName = $"Session #{session.GetId()}";
			if (session == currentSession) displayName += " (Current)";
			
			if (displayName == sessionName) {
				_selectedSession = session;
				UpdatePlayersList();
				break;
			}
		}
	}

		#endregion

		#region Session Operations

		private async UniTask SetCurrentSession(ISession session) {
			if (Main.Instance == null) {
				Logger.Log("No session manager instance available");
				return;
			}

			try {
				Logger.Log($"Setting session {session.GetId()} as current");
				await Main.Instance.SetCurrent(session.GetId());
				RefreshSessionsList();
			} catch (Exception ex) {
				Logger.LogError($"Failed to set current session: {ex.Message}");
				EditorUtility.DisplayDialog("Error", $"Failed to set current session: {ex.Message}", "OK");
			}
	}

	private async UniTask QuitSession() {
		if (_selectedSession == null) {
			Logger.Log("No session selected");
			return;
		}

		if (!EditorUtility.DisplayDialog(
			    "Confirm Quit",
			    $"Are you sure you want to quit session #{_selectedSession.GetId()}?",
			    "Quit",
			    "Cancel"
		    )) {
			return;
		}

		try {
			Logger.Log($"Quitting session {_selectedSession.GetId()}");
			
			// Get the adapter and try to disconnect/leave
			var adapter = _selectedSession.GetAdapter();
			if (adapter != null) {
				// Try to call Leave method via reflection if available
				var leaveMethod = adapter.GetType().GetMethod("Leave");
				if (leaveMethod != null) {
					var result = leaveMethod.Invoke(adapter, null);
					if (result is UniTask leaveTask) {
						await leaveTask;
					}
				}
			}
			
			RefreshSessionsList();
		} catch (Exception ex) {
			Logger.LogError($"Failed to quit session {_selectedSession.GetId()}: {ex.Message}");
			EditorUtility.DisplayDialog("Error", $"Failed to quit session: {ex.Message}", "OK");
		}
	}

	private void DisposeAllSessions()
			=> DisposeAllSessionsAsync().Forget();


		private async UniTask DisposeAllSessionsAsync() {
			var sessions = Main.Instance?.GetSessions();
			if (sessions == null || sessions.Length == 0) return;

			if (!EditorUtility.DisplayDialog(
				    "Confirm Disposal of All Sessions",
				    $"Are you sure you want to dispose all {sessions.Length} sessions?\nThis action cannot be undone.",
				    "Dispose All",
				    "Cancel"
			    )) {
				return;
			}

			try {
				foreach (var session in sessions) {
					await session.Dispose();
				}

				// Clean up all subscriptions
				_sessionEventsSubscriptions.Clear();

				RefreshSessionsList();
			} catch (Exception ex) {
				Logger.LogError($"Failed to dispose all sessions: {ex.Message}");
				EditorUtility.DisplayDialog("Error", $"Failed to dispose all sessions: {ex.Message}", "OK");
			}
		}

		#endregion

		#region Event Subscriptions

		private void SubscribeToSessionEvents(ISession session) {
			if (_sessionEventsSubscriptions.ContainsKey(session)) return;
			if (!(session is ISessionEvents sessionEvents)) return;

			_sessionEventsSubscriptions[session] = sessionEvents;

			sessionEvents.OnPlayerJoinedEvent().AddListener(_ => EditorApplication.delayCall += RefreshSessionsList);

			sessionEvents.OnPlayerLeftEvent().RemoveListener(_ => EditorApplication.delayCall += RefreshSessionsList);
		}

		#endregion

		#region Cleanup

		public void OnClosed() {
			// Cleanup when panel is closed
		}

		public void Dispose() {
			// Clean up event subscriptions
			_sessionEventsSubscriptions.Clear();

			if (_disposeAllButton != null) {
				_disposeAllButton.clicked -= DisposeAllSessions;
			}
		}

		#endregion
	}
}
#endif