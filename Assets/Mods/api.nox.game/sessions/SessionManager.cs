using System;
using System.Collections.Generic;
using UnityEngine;

namespace api.nox.game.sessions
{
    public class SessionManager : IDisposable
    {
        public GameSystem gameSystem;
        public List<Session> sessions = new();
        public ushort currentSessionUid = ushort.MaxValue;

        public byte NextId()
        {
            byte id = 0;
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
            foreach (var session in sessions.ToArray())
                session.Dispose();
            sessions.Clear();
        }

        internal Session GetSession(string group, uint id) => sessions.Find(session => session.group == group && session.id == id);
        internal Session CurrentSession
        {
            get => currentSessionUid == ushort.MaxValue ? null : sessions.Find(session => session.uid == currentSessionUid);
            set
            {
                if (value == null)
                {
                    var old = CurrentSession;
                    old?.OnDeselectedCurrent(null);
                    currentSessionUid = ushort.MaxValue;
                    gameSystem.OnSessionChanged(old, null);

                }
                else
                {
                    var session = GetSession(value.group, value.id);
                    if (session == null)
                        sessions.Add(value);
                    var old = CurrentSession;
                    old?.OnDeselectedCurrent(value);
                    currentSessionUid = value.uid;
                    value.OnSelectedCurrent(old);
                    gameSystem.OnSessionChanged(old, value);
                }
            }
        }

        internal Session New(SessionController controller, string group, uint id)
        {
            var session = new Session
            {
                uid = NextId(),
                id = id,
                group = group
            };
            session.controller = controller;
            sessions.Add(session);
            return session;
        }
    }
}