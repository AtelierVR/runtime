using System;
using Cysharp.Threading.Tasks;

namespace Nox.Servers {
	public interface IServerSocket {
		public UniTask<bool> Connect();
		public UniTask<bool> Close();

		public bool IsConnected();

		public bool CanAutoReconnect();
		public void SetAutoReconnect(bool autoReconnect);

		public UniTask Dispose();
	}
}