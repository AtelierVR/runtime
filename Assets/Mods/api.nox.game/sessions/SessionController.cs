using System;
using Cysharp.Threading.Tasks;

namespace api.nox.game.sessions
{
    public interface SessionController : IDisposable
    {
        public UniTask<bool> Prepare();
    }
}