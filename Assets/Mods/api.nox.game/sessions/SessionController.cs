using System;
using Cysharp.Threading.Tasks;

namespace api.nox.game.sessions
{
    public interface SessionController : IDisposable
    {
        Session session { get; set; }
        public UniTask<bool> Prepare();
    }
}