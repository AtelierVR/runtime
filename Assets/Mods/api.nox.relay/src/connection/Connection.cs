using Cysharp.Threading.Tasks;

namespace nox.nox.relay.connection
{
    public class Connection
    {
        public async UniTask Disconnect()
        {
            await UniTask.Yield();
        }
    }
}