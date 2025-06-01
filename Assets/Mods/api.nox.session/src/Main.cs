using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Players;
using Nox.Sessions;

namespace api.nox.session {
	public class Main : MainModInitializer, ISessionAPI {
		private readonly List<ISession> _sessions = new();

		private static MainModCoreAPI _coreAPI;

		public void OnInitializeMain(MainModCoreAPI api)
			=> _coreAPI = api;

		public async UniTask OnDisposeMainAsync() {
			foreach (var session in _sessions.ToArray())
				await session.Dispose();
			_sessions.Clear();
			_coreAPI = null;
		}

		[NoxPublic(NoxAccess.Method)]
		public ISession GetSession(int index) {
			if (index < 0 || index >= _sessions.Count) return null;
			return _sessions[index];
		}

		[NoxPublic(NoxAccess.Method)]
		public ISession New(IAdapter adapter)
			=> adapter != null
				? new Session(this, adapter)
				: null;

		internal void Add(Session session) {
			if (session == null) return;
			_sessions.Add(session);
			_coreAPI.EventAPI.Emit("session_added", session);
		}

		internal void Remove(Session session) {
			if (session == null) return;
			_sessions.Remove(session);
			_coreAPI.EventAPI.Emit("session_removed", session);
		}
	}
}