using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.Players;
using Nox.Sessions;

namespace api.nox.session {
	public class Main : MainModInitializer, ISessionAPI {
		private readonly List<ISession> _sessions = new();

		internal static MainModCoreAPI CoreAPI;

		internal static IEntityAPI EntityAPI
			=> CoreAPI.ModAPI.GetMod("entity").GetMains().FirstOrDefault() as IEntityAPI;

		public ISession GetSession(int index) {
			if (index < 0 || index >= _sessions.Count) return null;
			return _sessions[index];
		}

		public ISession MakeSession(IAdapter adapter) {
			if (adapter == null) return null;
			var session = new Session(adapter);
			_sessions.Add(session);
			return session;
		}
	}
}