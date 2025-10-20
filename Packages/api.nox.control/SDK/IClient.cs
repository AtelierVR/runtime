using System.Net;
using Cysharp.Threading.Tasks;

namespace Nox.SDK.Control {
	public interface IClient {
		public EndPoint GetEndPoint();

		public bool IsConnected();

		public UniTask Close();

		public IServer GetServer();

		public UniTask Send(string ev, params object[] args);
	}
}