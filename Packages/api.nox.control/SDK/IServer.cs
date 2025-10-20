using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Nox.SDK.Control {
	public interface IServer {
		public bool IsRunning();

		public void Start();

		public void Stop();

		public IClient[] GetClients();

		public UniTask Broadcast(string ev, params object[] args);

		public int GetPort();
	}
}