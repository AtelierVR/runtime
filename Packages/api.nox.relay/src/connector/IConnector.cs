using System.Net;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace api.nox.relay.connector {
	public interface IConnector {
		string GetProtocolName();

		bool          IsConnected();
		IPEndPoint    Remote();
		UniTask<bool> Connect(string    address, ushort port);
		void          SetBufferSize(int size);
		UniTask       Close();

		UnityEvent<Buffer> OnReceived { get; }

		UniTask<bool> Send(Buffer buffer);
		void          Update();

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