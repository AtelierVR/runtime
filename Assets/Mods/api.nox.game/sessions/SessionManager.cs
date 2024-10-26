using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace api.nox.game.sessions
{
    public class SessionManager
    {
        public static SessionManager Instance { get; private set; }
        public List<Session> sessions = new();
        public ushort currentSessionUid = ushort.MaxValue;

        [Serializable] public class SessionChangedEvent : UnityEvent<Session, Session> { }
        public SessionChangedEvent OnSessionChanged;

        public byte NextId()
        {
            byte id = 0;
            while (sessions.Exists(session => session.id == id))
                id++;
            return id;
        }

        public SessionManager()
        {
            if (Instance != null)
                throw new Exception("SessionManager already exists");
            Instance = this;
            if (OnSessionChanged == null)
                OnSessionChanged = new SessionChangedEvent();
            else OnSessionChanged.RemoveAllListeners();
        }

        internal void Update()
        {
            foreach (var session in sessions)
                session.Controller.Update();
        }

        public async UniTask Close()
        {
            Instance = null;
            OnSessionChanged.RemoveAllListeners();
            foreach (var session in sessions.ToArray())
                await session.Close();
            sessions.Clear();
            OnSessionChanged = null;
            sessions = null;
        }

        internal Session GetSession(string group, uint id) => sessions.Find(session => session.group == group && session.id == id);
        internal Session[] GetSessionsWithController<T>() where T : ISessionController
            => sessions.FindAll(session => session.Controller.GetType() == typeof(T)).ToArray();

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
                    OnSessionChanged?.Invoke(old, null);
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
                    OnSessionChanged?.Invoke(old, value);
                }
            }
        }

        internal Session New(ISessionController controller, string group, uint id)
        {
            var session = new Session
            {
                uid = NextId(),
                id = id,
                group = group
            };
            session.Controller = controller;
            sessions.Add(session);
            return session;
        }

        internal void Remove(Session session)
        {
            if (session == CurrentSession)
                CurrentSession = null;
            sessions.Remove(session);
        }
    }
}