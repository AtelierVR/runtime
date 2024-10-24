using System;
using Cysharp.Threading.Tasks;

namespace api.nox.game.sessions
{
    public interface ISessionController : IDisposable
    {
        public Session GetSession();
        internal void SetSession(Session session);
        public UniTask<bool> Prepare();
        public void Update() { }
    }
}