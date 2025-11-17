using System.Net;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace api.nox.relay.connector {
	public interface IConnector {
		/// <summary>
		/// Get the name of the protocol used by this connector.
		/// </summary>
		/// <returns></returns>
		string GetProtocolName();

		/// <summary>
		/// Check if the connector is connected.
		/// </summary>
		/// <returns></returns>
		bool IsConnected();

		/// <summary>
		/// Get the local endpoint of the connector.
		/// </summary>
		/// <returns></returns>
		IPEndPoint Remote();

		/// <summary>
		/// Connect to a remote address and port.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		UniTask<bool> Connect(string address, ushort port);

		/// <summary>
		/// Set the MTU size for the connector.
		/// </summary>
		/// <param name="size"></param>
		void SetMtuSize(int size);

		/// <summary>
		/// Get the MTU size for the connector.
		/// </summary>
		/// <returns></returns>
		int GetMtuSize();

		/// <summary>
		/// Close the connector.
		/// </summary>
		/// <returns></returns>
		UniTask Close();

		/// <summary>
		/// Event triggered when a buffer is received.
		/// </summary>
		UnityEvent<Buffer> OnReceived { get; }

		/// <summary>
		/// Send a buffer through the connector.
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		UniTask<bool> Send(Buffer buffer);

		/// <summary>
		/// Update the connector.
		/// </summary>
		void Update();

		/// <summary>
		/// Try to parse an IPEndPoint from a string in the format "ip:port".
		/// </summary>
		/// <param name="input"></param>
		/// <param name="endPoint"></param>
		/// <returns></returns>
		public static bool TryParseIPEndPoint(string input, out IPEndPoint endPoint) {
			endPoint = null;
			if (string.IsNullOrWhiteSpace(input))
				return false;

			var parts = input.Split(':');
			if (parts.Length != 2)
				return false;

			if (!IPAddress.TryParse(parts[0], out var ip))
				return false;

			if (!ushort.TryParse(parts[1], out var port))
				return false;

			endPoint = new IPEndPoint(ip, port);
			return true;
		}
	}
}