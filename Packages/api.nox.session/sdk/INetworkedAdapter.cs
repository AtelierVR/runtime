/*using System;
using Cysharp.Threading.Tasks;

namespace Nox.Sessions {
	public interface INetworkedAdapter {
		/// <summary>
		/// Check if the adapter is connected.
		/// </summary>
		/// <returns></returns>
		public bool IsConnected();

		/// <summary>
		/// Emit an event to the adapter.
		/// </summary>
		/// <param name="event"></param>
		/// <param name="raw"></param>
		/// <returns></returns>
		public UniTask<bool> EmitEvent(string @event, byte[] raw);

		/// <summary>
		/// Get the current time from the adapter.
		/// </summary>
		/// <returns></returns>
		public DateTime GetTime();

		/// <summary>
		/// Get the current ping from the adapter.
		/// </summary>
		/// <returns></returns>
		public double GetLatency();
		
		/// <summary>
		/// Get the current ticks per second from the adapter.
		/// </summary>
		/// <returns></returns>
		public int GetTps();

		/// <summary>
		/// Get the current threshold from the adapter.
		/// </summary>
		/// <returns></returns>
		public double GetThreshold();
	}
}*/