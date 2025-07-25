using Cysharp.Threading.Tasks;

namespace Nox.Servers {
	public interface IServerAPI {
		public UniTask<IServer> Fetch(string address);

		public UniTask<IServerSocket> Connect(string address);
	}
}