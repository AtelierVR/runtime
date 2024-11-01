using System;
using Cysharp.Threading.Tasks;

namespace api.nox.game.sessions
{
    public class OfflineController : ISessionController
    {
        public string SessionType => "offline";
        internal Session _session;
        public Session GetSession() => _session;

        internal void SetSession(Session session) => _session = session;
        void ISessionController.SetSession(Session session) => SetSession(session);

        public UniTask<bool> Prepare()
        {
            return UniTask.FromResult(true);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async UniTask Close()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            return;
        }
    }
}