using System;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

namespace Nox.Servers {
	public interface IServerSocket {
		public UniTask<bool> Connect();
		public UniTask<bool> Close();

		public bool IsConnected();

		public bool CanAutoReconnect();
		public void SetAutoReconnect(bool autoReconnect);

		public UniTask Dispose();

		UnityEvent OnConnected { get; }
		UnityEvent OnDisconnected { get; }
		UnityEvent<byte[]> OnRaw { get; }
		UnityEvent<SocketPacket> OnPacket { get; }

		public UniTask Emit<T>(SocketPacket<T> packet);

		public void On<T>(string type, Action<SocketPacket<T>> handler);
		public void Off<T>(string type, Action<SocketPacket<T>> handler);
		public void Once<T>(string type, Action<SocketPacket<T>> handler);
	}
}
