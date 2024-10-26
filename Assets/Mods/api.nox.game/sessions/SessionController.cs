using System;
using Cysharp.Threading.Tasks;

namespace api.nox.game.sessions
{
    public interface ISessionController
    {
        public Session GetSession();
        internal void SetSession(Session session);
        public UniTask<bool> Prepare();
        public UniTask Close();
        public void Update() { }
    }
}