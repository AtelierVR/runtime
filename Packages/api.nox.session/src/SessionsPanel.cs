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
		private          MultiColumnListView _sessionsListView;
		private          IntegerField        _sessionCountField;
		private          TextField           _currentSessionField;
		private          Button              _disposeAllButton;
		private          HelpBox             _statusHelpBox;

		// Data tracking
		private readonly Dictionary<ISession, ISessionEvents> _sessionEventsSubscriptions = new();
		private readonly List<SessionDisplayData>             _sessionItems               = new();

		#endregion

		#region ListView Data Classes

		private class SessionDisplayData {
			public ISession Session     { get; }
			public string   DisplayName { get; set; }
			public bool     IsCurrent   { get; set; }
			public string   AdapterType { get; set; }
			public int      PlayerCount { get; set; }
			public string   Status      { get; set; }
			public string   PlayersInfo { get; set; }

			public SessionDisplayData(ISession session, bool isCurrent) {
				Session   = session;
				IsCurrent = isCurrent;
				UpdateData();
			}

			public void UpdateData() {
				var adapter = Session?.GetAdapter();
				DisplayName = $"Session #{Session?.GetId()}";
				AdapterType = adapter?.GetType().Name   ?? "Unknown";
				PlayerCount = adapter?.GetPlayerCount() ?? 0;
				Status      = IsCurrent ? "Current" : "Active";

				// Build players info string
				if (adapter != null && PlayerCount > 0) {
					var players = new List<string>();
					for (int i = 0; i < PlayerCount; i++) {
						var player = adapter.GetPlayer(i);
						if (player != null) {
							var name = player.ToIdentifier()?.ToString() ?? $"Player {i}";
							var type = "";
							if (player      == adapter.GetLocalPlayer()) type  = " (Local)";
							else if (player == adapter.GetMasterPlayer()) type = " (Master)";
							players.Add(name + type);
						}
					}

					PlayersInfo = string.Join(", ", players);
				} else {
					PlayersInfo = "No players";
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
			if (_sessionsListView == null) return;

			_sessionItems.Clear();

			// Sort sessions: current first, then by ID
			var sortedSessions = sessions.OrderByDescending(s => s == currentSession)
				.ThenBy(s => s.GetId())
				.ToArray();

			foreach (var session in sortedSessions) {
				var item = new SessionDisplayData(session, session == currentSession);
				_sessionItems.Add(item);

				// Subscribe to events for real-time updates
				SubscribeToSessionEvents(session);
			}

			_sessionsListView.itemsSource = _sessionItems;
			_sessionsListView.RefreshItems();
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
			_sessionCountField = _root.Q<IntegerField>("session-count-field");
			_currentSessionField = _root.Q<TextField>("current-session-field");
			_disposeAllButton = _root.Q<Button>("dispose-all-button");
			_statusHelpBox = _root.Q<HelpBox>("status-helpbox");
			
			// Setup button event
			if (_disposeAllButton != null) {
				_disposeAllButton.clicked += DisposeAllSessions;
			}
		}

		private void SetupProgrammaticElements() {
			// Create and add the MultiColumnListView programmatically
			var sessionsContainer = _root.Q<VisualElement>("sessions-list-container");
			if (sessionsContainer != null) {
				CreateSessionsListView(sessionsContainer);
			}
		}

		private void CreateLayoutProgrammatically() {
			// Fallback method - same as before
			CreateSessionInfoSection();
			CreateGlobalActionsSection();
			CreateStatusSection();
			CreateSessionsListView();
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

		private void CreateSessionsListView(VisualElement container = null) {
			// Create columns first with smaller widths
			var columns = new Columns();
			columns.Add(new Column { title = "Session", width = 80, minWidth = 60 });
			columns.Add(new Column { title = "Adapter", width = 80, minWidth = 60 });
			columns.Add(new Column { title = "Players", width = 50, minWidth = 40 });
			columns.Add(new Column { title = "Status", width = 60, minWidth = 50 });
			columns.Add(new Column { title = "Players Info", width = 150, minWidth = 100 });
			columns.Add(new Column { title = "Actions", width = 100, minWidth = 80 });

			// Create ListView with columns
			_sessionsListView = new MultiColumnListView(columns);
			_sessionsListView.showBorder = true;
			_sessionsListView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
			_sessionsListView.fixedItemHeight = 20;

			// Set up column renderers
			if (_sessionsListView.columns != null) {
				var sessionColumn = _sessionsListView.columns["Session"];
				if (sessionColumn != null) {
					sessionColumn.makeCell = () => new Label();
					sessionColumn.bindCell = (element, index) => {
						if (element is Label label && index < _sessionItems.Count) {
							label.text = _sessionItems[index].DisplayName;
						}
					};
				}

				var adapterColumn = _sessionsListView.columns["Adapter"];
				if (adapterColumn != null) {
					adapterColumn.makeCell = () => new Label();
					adapterColumn.bindCell = (element, index) => {
						if (element is Label label && index < _sessionItems.Count) {
							label.text = _sessionItems[index].AdapterType;
						}
					};
				}

				var playersColumn = _sessionsListView.columns["Players"];
				if (playersColumn != null) {
					playersColumn.makeCell = () => new Label();
					playersColumn.bindCell = (element, index) => {
						if (element is Label label && index < _sessionItems.Count) {
							label.text = _sessionItems[index].PlayerCount.ToString();
						}
					};
				}

				var statusColumn = _sessionsListView.columns["Status"];
				if (statusColumn != null) {
					statusColumn.makeCell = () => new Label();
					statusColumn.bindCell = (element, index) => {
						if (element is Label label && index < _sessionItems.Count) {
							label.text = _sessionItems[index].Status;
						}
					};
				}

				var playersInfoColumn = _sessionsListView.columns["Players Info"];
				if (playersInfoColumn != null) {
					playersInfoColumn.makeCell = () => new Label();
					playersInfoColumn.bindCell = (element, index) => {
						if (element is Label label && index < _sessionItems.Count) {
							label.text = _sessionItems[index].PlayersInfo;
						}
					};
				}

				var actionsColumn = _sessionsListView.columns["Actions"];
				if (actionsColumn != null) {
					actionsColumn.makeCell = () => CreateActionButtons();
					actionsColumn.bindCell = (element, index) => {
						if (element is VisualElement container && index < _sessionItems.Count) {
							BindActionButtons(container, _sessionItems[index]);
						}
					};
				}
			}

			// Add to container or create foldout
			if (container != null) {
				container.Add(_sessionsListView);
			} else {
				var foldout = new Foldout { text = "Sessions", value = true };
				foldout.Add(_sessionsListView);
				_root.Add(foldout);
			}
		}

		private VisualElement CreateActionButtons() {
			var container = new VisualElement();
			container.style.flexDirection = FlexDirection.Row;

			var setCurrentButton = new Button { text = "Set Current" };
			setCurrentButton.name = "setCurrentButton";
			container.Add(setCurrentButton);

			var disposeButton = new Button { text = "Dispose" };
			disposeButton.name = "disposeButton";
			container.Add(disposeButton);

			return container;
		}

		private void BindActionButtons(VisualElement container, SessionDisplayData item) {
			var setCurrentButton = container.Q<Button>("setCurrentButton");
			var disposeButton    = container.Q<Button>("disposeButton");

			if (setCurrentButton != null && disposeButton != null) {
				setCurrentButton.SetEnabled(!item.IsCurrent);

				// Clear existing callbacks and add new ones
				setCurrentButton.clicked -= null;
				disposeButton.clicked    -= null;

				setCurrentButton.clicked += () => SetCurrentSession(item.Session).Forget();
				disposeButton.clicked    += () => DisposeSession(item.Session).Forget();
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

		private async UniTask DisposeSession(ISession session) {
			if (!EditorUtility.DisplayDialog(
				    "Confirm Disposal",
				    $"Are you sure you want to dispose session #{session.GetId()}?\nThis action cannot be undone.",
				    "Dispose",
				    "Cancel"
			    )) {
				return;
			}

			try {
				await session.Dispose();

				// Clean up subscriptions
				_sessionEventsSubscriptions.Remove(session);

				RefreshSessionsList();
			} catch (Exception ex) {
				Logger.LogError($"Failed to dispose session {session.GetId()}: {ex.Message}");
				EditorUtility.DisplayDialog("Error", $"Failed to dispose session: {ex.Message}", "OK");
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

			sessionEvents.AddPlayerJoinedListener((_) => { EditorApplication.delayCall += RefreshSessionsList; });

			sessionEvents.AddPlayerLeftListener((_) => { EditorApplication.delayCall += RefreshSessionsList; });
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

