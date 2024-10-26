using System;
using Cysharp.Threading.Tasks;

namespace api.nox.game.sessions
{
    public class OfflineController : ISessionController
    {
        internal Session _session;
        public Session GetSession() => _session;
        internal void SetSession(Session session) => _session = session;
        void ISessionController.SetSession(Session session) => SetSession(session);

        public UniTask<bool> Prepare()
        {
            return UniTask.FromResult(true);
        }

        public async UniTask Close()
        {
            return;
        }
    }
}