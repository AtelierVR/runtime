using Cysharp.Threading.Tasks;

namespace api.nox.game.sessions
{
    public class OfflineController : SessionController
    {
        public void Dispose()
        {
        }

        public UniTask<bool> Prepare()
        {
            return UniTask.FromResult(true);
        }
    }
}