using Cysharp.Threading.Tasks;

namespace api.nox.game.sessions
{
    public class OfflineController : SessionController
    {
        public Session _session;
        public Session session
        {
            get => _session;
            set => _session = value;
        }

        public void Dispose()
        {
        }

        public UniTask<bool> Prepare()
        {
            return UniTask.FromResult(true);
        }
    }
}