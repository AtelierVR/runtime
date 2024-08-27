using System;
using System.Collections.Generic;

namespace api.nox.game.sessions
{
    public class SessionManager : IDisposable
    {
        public GameSystem gameSystem;
        public List<Session> sessions = new();
        public ushort currentSessionUid;

        public ushort NextId()
        {
            ushort id = 0;
            while (sessions.Exists(session => session.id == id))
                id++;
            return id;
        }

        public SessionManager(GameSystem gameSystem)
        {
            this.gameSystem = gameSystem;

        }

        public void Dispose()
        {
            foreach (var session in sessions)
                session.Dispose();
            sessions.Clear();
        }

        internal Session GetSession(string group, uint id) => sessions.Find(session => session.group == group && session.id == id);
        internal Session CurrentSession
        {
            get => sessions.Find(session => session.uid == currentSessionUid);
            set
            {
                var session = GetSession(value.group, value.id);
                if (session == null)
                    sessions.Add(value);
                currentSessionUid = value.uid;
            }
        }

        internal Session New(SessionController controller, string group, uint id)
        {
            var session = new Session
            {
                uid = NextId(),
                id = (ushort)id,
                group = group,
                controller = controller
            };
            sessions.Add(session);
            return session;
        }
    }
}