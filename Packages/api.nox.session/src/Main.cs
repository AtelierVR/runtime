using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.session.commands;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Controllers;
using Nox.Sessions;
using UnityEngine;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.session {
	public class Main : IMainModInitializer, ISessionAPI {
		private readonly List<ISession> _sessions = new();
		internal         MainModCoreAPI CoreAPI;
		internal static  Main           Instance;
		private          ushort         _nextId   = ushort.MinValue + 1;
		internal         ushort         CurrentId = ushort.MinValue;
		private          GameObject     _updateHandler;
		private          LanguagePack   _lang;
		private          Commands       _commands;

		public static readonly UnityEvent<ISession>           OnSessionAdded   = new();
		public static readonly UnityEvent<ISession>           OnSessionRemoved = new();
		public static readonly UnityEvent<ISession, ISession> OnCurrentChanged = new();

		internal IControllerAPI ControllerAPI
			=> CoreAPI.ModAPI.GetMod("controller")
				?.GetInstance<IControllerAPI>();

		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			_lang   = CoreAPI.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);
			_commands = new Commands();
		}

		public async UniTask OnDisposeMainAsync() {
			_commands.Dispose();
			_commands = null;
			foreach (var session in _sessions.ToArray())
				await session.Dispose();
			_sessions.Clear();
			LanguageManager.RemovePack(_lang);
			_lang    = null;
			CoreAPI  = null;
			Instance = null;
		}

		public void OnUpdateMain() {
			foreach (var session in _sessions.ToArray())
				session.OnUpdate();
		}

		[NoxPublic(NoxAccess.Method)]
		public ISession GetSession(ushort id)
			=> _sessions.FirstOrDefault(s => s.GetId() == id);

		[NoxPublic(NoxAccess.Method)]
		public ISession[] GetSessions()
			=> _sessions.Cast<ISession>().ToArray();

		[NoxPublic(NoxAccess.Method)]
		public int GetSessionCount()
			=> _sessions.Count;

		[NoxPublic(NoxAccess.Method)]
		public ISession New(IAdapter adapter)
			=> adapter != null
				? new Session(this, GetNextId(), adapter)
				: null;

		[NoxPublic(NoxAccess.Method)]
		public ISession GetCurrent()
			=> GetSession(CurrentId);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask SetCurrent(ushort id) {
			if (id == CurrentId) return;
			var nSession = _sessions.FirstOrDefault(s => s.GetId() == id);
			var oSession = _sessions.FirstOrDefault(s => s.GetId() == CurrentId);

			if (oSession != null)
				await oSession.OnDeselect(nSession);
			CurrentId = id;
			if (nSession != null)
				await nSession.OnSelect(oSession);

			var local = nSession?.GetAdapter()?.GetLocalPlayer();
			ControllerAPI.GetCurrent()?.SetPlayer(local);

			CoreAPI.EventAPI.Emit("session_current_changed", nSession, oSession);
			OnCurrentChanged?.Invoke(nSession, oSession);
		}

		internal void Add(Session session) {
			if (session == null) return;
			_sessions.Add(session);
			CoreAPI.EventAPI.Emit("session_added", session);
			OnSessionAdded?.Invoke(session);
		}

		internal void Remove(Session session) {
			if (session == null) return;
			_sessions.Remove(session);
			CoreAPI.EventAPI.Emit("session_removed", session);
			OnSessionRemoved?.Invoke(session);
		}

		private ushort GetNextId() {
			var i = _nextId;
			do {
				if (i >= ushort.MaxValue) i = ushort.MinValue;
				else i++;
			} while (_sessions.Any(s => s.GetId() == i));

			return _nextId = i;
		}

		[NoxPublic(NoxAccess.Method)]
		public bool CanMakeSession(string adapterId, Dictionary<string, object> options = null) {
			var canMake = false;
			options ??= new Dictionary<string, object>();
			CoreAPI.EventAPI.Emit("session_can_make_adapter", adapterId, options, new Action<object[]>(Callback));
			return canMake;

			void Callback(object[] data) {
				if (data is { Length: > 0 } && data[0] is true)
					canMake = true;
			}
		}

		[NoxPublic(NoxAccess.Method)]
		public ISession MakeSession(string adapterId, Dictionary<string, object> options = null) {
			if (!CanMakeSession(adapterId, options)) return null;
			IAdapter adapter = null;
			ISession session = null;
			CoreAPI.EventAPI.Emit("session_make_adapter", adapterId, options, new Action<object[]>(Callback));
			if (adapter == null) {
				Logger.LogError($"Failed to make session with adapter '{adapterId}'");
				return null;
			}

			session ??= New(adapter);
			if (session != null)
				return session;

			Logger.LogError($"Failed to create session with adapter '{adapterId}'");
			return null;

			void Callback(object[] data) {
				adapter = data is { Length: > 0 } && data[0] is IAdapter a ? a : null;
				session = data is { Length: > 1 } && data[1] is ISession s ? s : null;
			}
		}
	}
}