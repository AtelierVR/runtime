using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using Nox.Controllers;
using Nox.Sessions;
using UnityEngine;

namespace api.nox.session {
	public class Main : MainModInitializer, ISessionAPI {
		private readonly List<Session>  _sessions = new();
		internal         MainModCoreAPI CoreAPI;
		internal static  Main           Instance;
		private          ushort         _nextId    = ushort.MinValue + 1;
		private          ushort         _currentId = ushort.MinValue;
		private          GameObject     _updateHandler;

		internal IControllerAPI ControllerAPI
			=> CoreAPI.ModAPI.GetMod("controller")
				?.GetMains()
				.FirstOrDefault() as IControllerAPI;

		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
		}

		public async UniTask OnDisposeMainAsync() {
			foreach (var session in _sessions.ToArray())
				await session.Dispose();
			_sessions.Clear();

			CoreAPI  = null;
			Instance = null;
		}

		[NoxPublic(NoxAccess.Method)]
		public ISession GetSession(ushort id)
			=> _sessions.FirstOrDefault(s => s.Id == id);

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
			=> GetSession(_currentId);

		[NoxPublic(NoxAccess.Method)]
		public void SetCurrent(ushort id) {
			if (id == _currentId) return;
			var nSession = _sessions.FirstOrDefault(s => s.Id == id);
			var oSession = _sessions.FirstOrDefault(s => s.Id == _currentId);

			oSession?.OnDeselect(nSession);
			_currentId = id;
			nSession?.OnSelect(oSession);

			if (nSession != null)
				ControllerAPI.GetCurrent()
					.SetPlayer(nSession.GetAdapter().GetLocalPlayer());

			CoreAPI.EventAPI.Emit("session_current_changed", nSession, oSession);
		}

		internal void Add(Session session) {
			if (session == null) return;
			_sessions.Add(session);
			CoreAPI.EventAPI.Emit("session_added", session);
		}

		internal void Remove(Session session) {
			if (session == null) return;
			_sessions.Remove(session);
			CoreAPI.EventAPI.Emit("session_removed", session);
		}

		private ushort GetNextId() {
			var i = _nextId;
			do {
				if (i >= ushort.MaxValue) i = ushort.MinValue + 1;
				else i++;
			} while (_sessions.Any(s => s.Id == i));

			return _nextId = i;
		}
	}
}