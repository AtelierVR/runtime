#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Panels;
using Nox.Sessions;
using Nox.Players;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.session {
	public class SessionsPanel : IEditorPanelBuilder, IDisposable {
		public string GetId()
			=> "sessions";

		public string GetName()
			=> "Sessions/Manager";

		public string GetTitle()
			=> LanguageManager.Get("session.manager.title");

		public bool IsHidden()
			=> false;

		public VisualElement[] GetHeaders() {
			var refreshButton = new Button { text = LanguageManager.Get("session.manager.refresh") };
			refreshButton.AddToClassList("nox-transparent");
			refreshButton.RegisterCallback<ClickEvent>(OnRefresh);
			return new VisualElement[] { refreshButton };
		}

		private void OnRefresh(ClickEvent evt) {
			RefreshSessionsList();
			evt.StopPropagation();
		}

		private readonly VisualElement _root = new();
		private          VisualElement _sessionsList;
		private          Label         _sessionsCountLabel;
		private          Label         _currentSessionLabel;
		private          Button        _disposeAllButton;
		
		// Track session events subscriptions for cleanup
		private readonly Dictionary<ISession, ISessionEvents> _sessionEventsSubscriptions = new();

		internal void Update() {
			if (!Editor.HasSessionPanelOpened() || _root.childCount == 0) return;
			RefreshSessionsList();
		}

		private void RefreshSessionsList() {
			if (Main.Instance == null) return;

			var sessions       = Main.Instance.GetSessions();
			var currentSession = Main.Instance.GetCurrent();
			var sessionCount   = Main.Instance.GetSessionCount();

			// Update sessions count
			if (_sessionsCountLabel != null)
				_sessionsCountLabel.text = LanguageManager.Get("session.manager.sessions_count", sessionCount);

			// Update current session info
			if (_currentSessionLabel != null) {
				if (currentSession != null) {
					var adapter     = currentSession.GetAdapter();
					var adapterName = adapter?.GetType().Name ?? LanguageManager.Get("session.manager.unknown");
					_currentSessionLabel.text = LanguageManager.Get("session.manager.current_session") + $" #{currentSession.GetId()} - " + LanguageManager.Get("session.manager.adapter_label", adapterName);
				} else {
					_currentSessionLabel.text = LanguageManager.Get("session.manager.no_current_session");
				}
			}

			// Update dispose all button
			if (_disposeAllButton != null)
				_disposeAllButton.SetEnabled(sessionCount > 0); // Update sessions list
			UpdateSessionsList(sessions, currentSession);
		}

		private void UpdateSessionsList(ISession[] sessions, ISession currentSession) {
			if (_sessionsList == null) return;

			_sessionsList.Clear();

			if (sessions.Length == 0) {
				var noSessionsLabel = new Label(LanguageManager.Get("session.manager.no_sessions"));
				noSessionsLabel.AddToClassList("session-no-sessions");
				_sessionsList.Add(noSessionsLabel);
				return;
			}

			foreach (var session in sessions) {
				var sessionItem = CreateSessionItem(session, session == currentSession);
				_sessionsList.Add(sessionItem);
			}
		}

		private VisualElement CreateSessionItem(ISession session, bool isCurrent) {
			var container = new VisualElement();
			container.AddToClassList("session-item");
			if (isCurrent) container.AddToClassList("session-current");

			// Session header
			var header = new VisualElement();
			header.AddToClassList("session-header");

			var titleLabel = new Label($"Session #{session.GetId()}");
			titleLabel.AddToClassList("session-title");
			if (isCurrent) {
				titleLabel.text += " " + LanguageManager.Get("session.manager.current_indicator");
			}

			header.Add(titleLabel);

			// Session info
			var infoContainer = new VisualElement();
			infoContainer.AddToClassList("session-info");

			var adapter = session.GetAdapter();
			if (adapter != null) {
				var adapterLabel = new Label(LanguageManager.Get("session.manager.adapter_label", adapter.GetType().Name));
				adapterLabel.AddToClassList("session-adapter");
				infoContainer.Add(adapterLabel);

				// Player count
				var localPlayer  = adapter.GetLocalPlayer();
				var masterPlayer = adapter.GetMasterPlayer();
				var playersInfo = new Label(
					LanguageManager.Get(
						"session.manager.players_info",
						new object[] {
							localPlayer != null ? LanguageManager.Get("session.manager.true") : LanguageManager.Get("session.manager.false"),
							masterPlayer?.ToIdentifier()?.ToString() ?? LanguageManager.Get("session.manager.none")
						}
					)
				);
				playersInfo.AddToClassList("session-players");
				infoContainer.Add(playersInfo);

				// Add players list if session supports ISessionEvents
				if (session is ISessionEvents sessionEvents) {
					var playersListContainer = CreatePlayersListContainer(adapter, sessionEvents, session);
					infoContainer.Add(playersListContainer);
				}
			}

			// Actions
			var actionsContainer = new VisualElement();
			actionsContainer.AddToClassList("session-actions");

			if (!isCurrent) {
				var setCurrentButton = new Button() { text = LanguageManager.Get("session.manager.set_current") };
				setCurrentButton.RegisterCallback<ClickEvent>(_ => SetCurrentSession(session).Forget());
				setCurrentButton.AddToClassList("session-action-button");
				actionsContainer.Add(setCurrentButton);
			}

			var disposeButton = new Button { text = LanguageManager.Get("session.manager.dispose") };
			disposeButton.RegisterCallback<ClickEvent>(_ => DisposeSession(session).Forget());
			disposeButton.AddToClassList("session-action-button");
			disposeButton.AddToClassList("session-dispose-button");
			actionsContainer.Add(disposeButton);

			container.Add(header);
			container.Add(infoContainer);
			container.Add(actionsContainer);

			return container;
		}

		private VisualElement CreatePlayersListContainer(IAdapter adapter, ISessionEvents sessionEvents, ISession session) {
			var playersContainer = new VisualElement();
			playersContainer.AddToClassList("session-players-list");

			var playersHeader = new Label("Players:");
			playersHeader.AddToClassList("session-players-header");
			playersContainer.Add(playersHeader);

			var playersList = new VisualElement();
			playersList.AddToClassList("session-players-items");
			
			// Initial population of players list
			UpdatePlayersList(playersList, adapter);
			
			playersContainer.Add(playersList);

			// Subscribe to session events if not already subscribed
			if (!_sessionEventsSubscriptions.ContainsKey(session)) {
				_sessionEventsSubscriptions[session] = sessionEvents;
				
				// Subscribe to player events to update the list in real-time
				sessionEvents.AddPlayerJoinedListener((player) => {
					EditorApplication.delayCall += () => UpdatePlayersList(playersList, adapter);
				});
				
				sessionEvents.AddPlayerLeftListener((player) => {
					EditorApplication.delayCall += () => UpdatePlayersList(playersList, adapter);
				});
			}

			return playersContainer;
		}

		private void UpdatePlayersList(VisualElement playersList, IAdapter adapter) {
			if (playersList == null || adapter == null) return;

			playersList.Clear();

			var playerCount = adapter.GetPlayerCount();
			if (playerCount == 0) {
				var noPlayersLabel = new Label("No players");
				noPlayersLabel.AddToClassList("session-no-players");
				playersList.Add(noPlayersLabel);
				return;
			}

			for (int i = 0; i < playerCount; i++) {
				var player = adapter.GetPlayer(i);
				if (player != null) {
					var playerItem = new VisualElement();
					playerItem.AddToClassList("session-player-item");

					var playerLabel = new Label($"• {player.ToIdentifier()?.ToString() ?? $"Player {i}"}");
					playerLabel.AddToClassList("session-player-name");
					
					// Mark local and master players
					var localPlayer = adapter.GetLocalPlayer();
					var masterPlayer = adapter.GetMasterPlayer();
					
					if (player == localPlayer) {
						playerLabel.text += " (Local)";
						playerLabel.AddToClassList("session-player-local");
					}
					
					if (player == masterPlayer) {
						playerLabel.text += " (Master)";
						playerLabel.AddToClassList("session-player-master");
					}

					playerItem.Add(playerLabel);
					playersList.Add(playerItem);
				}
			}
		}

		private async UniTask SetCurrentSession(ISession session) {
			if (Main.Instance == null) {
				Logger.Log("No session manager instance available");
				return;
			}

			Logger.Log("Setting current session");
			await Main.Instance.SetCurrent(session.GetId());
			RefreshSessionsList();
		}

		private async UniTask DisposeSession(ISession session) {
			if (EditorUtility.DisplayDialog(
				    LanguageManager.Get("session.manager.dispose_confirm_title"),
				    LanguageManager.Get("session.manager.dispose_confirm_message", session.GetId()),
				    LanguageManager.Get("session.manager.dispose"),
				    LanguageManager.Get("session.manager.cancel")
			    )) {
				try {
					await session.Dispose();
					RefreshSessionsList();
				} catch (Exception ex) {
					Logger.LogError($"Failed to dispose session {session.GetId()}: {ex.Message}");
					EditorUtility.DisplayDialog(
						LanguageManager.Get("session.manager.error"),
						LanguageManager.Get("session.manager.dispose_error", ex.Message),
						LanguageManager.Get("session.manager.ok")
					);
				}
			}
		}

		private async void DisposeAllSessions() {
			if (EditorUtility.DisplayDialog(
				    LanguageManager.Get("session.manager.dispose_all_confirm_title"),
				    LanguageManager.Get("session.manager.dispose_all_confirm_message"),
				    LanguageManager.Get("session.manager.dispose_all"),
				    LanguageManager.Get("session.manager.cancel")
			    )) {
				try {
					var sessions = Main.Instance?.GetSessions();
					if (sessions != null) {
						foreach (var session in sessions) {
							await session.Dispose();
						}
					}

					RefreshSessionsList();
				} catch (Exception ex) {
					Logger.LogError($"Failed to dispose all sessions: {ex.Message}");
					EditorUtility.DisplayDialog(
						LanguageManager.Get("session.manager.error"),
						LanguageManager.Get("session.manager.dispose_all_error", ex.Message),
						LanguageManager.Get("session.manager.ok")
					);
				}
			}
		}

		public VisualElement Make(Dictionary<string, object> data) {
			_root.ClearBindings();
			_root.Clear();

			// Reset cached UI elements
			_sessionsList        = null;
			_sessionsCountLabel  = null;
			_currentSessionLabel = null;
			_disposeAllButton    = null;

			var child = Editor.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("sessions.uxml")?.CloneTree();
			if (child == null) throw new Exception("Failed to load sessions panel UI from 'sessions.uxml'");

			child.style.flexGrow = 1;
			_root.Add(child);

			// Cache UI elements
			_sessionsList        = _root.Q<VisualElement>("sessions-list");
			_sessionsCountLabel  = _root.Q<Label>("sessions-count");
			_currentSessionLabel = _root.Q<Label>("current-session");
			_disposeAllButton    = _root.Q<Button>("dispose-all-button");

			// Initialize components
			if (_disposeAllButton != null) {
				_disposeAllButton.clicked += DisposeAllSessions;
			}

			// Initial refresh
			RefreshSessionsList();

			return _root;
		}

		public void OnClosed() {
			// Cleanup when panel is closed
		}

		public void Dispose() {
			// Cleanup session events subscriptions
			foreach (var kvp in _sessionEventsSubscriptions) {
				var session = kvp.Key;
				var sessionEvents = kvp.Value;
				
				// Note: We can't unsubscribe specific listeners without keeping references to them
				// This is a limitation of the current ISessionEvents interface
				// In a real implementation, you might want to keep references to the specific actions
			}
			_sessionEventsSubscriptions.Clear();

			// Clear cached references
			_sessionsList        = null;
			_sessionsCountLabel  = null;
			_currentSessionLabel = null;
			_disposeAllButton    = null;

			// Clear UI
			_root.Clear();
			_root.ClearBindings();
		}
	}
}
#endif

