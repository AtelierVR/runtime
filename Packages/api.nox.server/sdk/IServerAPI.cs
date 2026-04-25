using Cysharp.Threading.Tasks;
using UnityEngine.Events;

namespace Nox.Servers {
	public interface IServerAPI {
		public UniTask<IServer> Fetch(string address);

		public UniTask<IServerSocket> Connect(string address);

		public IServerSocket Current { get; }

		UnityEvent OnSocketConnected { get; }
		UnityEvent OnSocketDisconnected { get; }
	}
}