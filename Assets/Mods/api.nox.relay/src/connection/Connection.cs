using System;
using System.Globalization;
using api.nox.relay.connector;
using api.nox.relay.types;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.connection {
	public class Connection : INoxObject {
		public const ushort ProtocolVersion = 1;

		public static Connection New<T>() where T : IConnector, new() {
			var connection = new Connection(new T());
			RelaySystem.Instance.Connections.Add(connection);
			return connection;
		}

		public readonly ushort     Id;
		public readonly IConnector Connector;

		private Connection(IConnector connector) {
			Id                        =  RelaySystem.Instance.NextId();
			Connector                 =  connector;
			Connector.OnReceivedEvent += OnReceived;
		}

		public async UniTask<bool> Connect(string address, ushort port)
			=> await Connector.Connect(address, port);

		private void OnReceived(Buffer buffer) {
			if (buffer.length < 5) return;
			buffer.Goto(0);
			var length = buffer.ReadUShort();
			var state  = buffer.ReadUShort();
			var type   = buffer.Read<ResponseType>();
			if (length < 5 || length > buffer.length) return;
			if (type != ResponseType.Latency)
				Logger.Log($"Received {state} {type} from {Connector.Remote()}");
			if (length < 6) return;
			switch (type) {
				case ResponseType.Enter:
				case ResponseType.Quit:
				case ResponseType.Join:
				case ResponseType.Leave:
				case ResponseType.Teleport:
				case ResponseType.Transform:
				case ResponseType.CustomDataPacket:
					var iid = buffer.ReadUShort();
					// var instance = RelayInstanceManager.Get(iid, Id);
					// instance?.OnInstanceEventInvoke(buffer.Clone(5, (ushort)(length - 5)));
					break;
				case ResponseType.Disconnect:
					var message = buffer.ReadString();
					Logger.Log($"Received disconnect message: {message}");
					_lastHandshake = null;
					_lastLatency   = null;
					break;
			}
		}

		private DateTime                           _lastLatencyRequest = DateTime.MinValue;
		private types.Handshakes.RelayResponseHandshake _lastHandshake;
		private types.Latency.RelayResponseLatency      _lastLatency;
		public  types.Session.RelayResponseSessions     _lastSessions;

		public ClientStatus Status
			=> _lastHandshake?.Status ?? ClientStatus.Disconnected;

		public ushort ClientId
			=> _lastHandshake?.ClientId ?? ushort.MaxValue;

		public async UniTask Disconnect() {
			// Disconnect logic here
			await UniTask.Yield();
		}

		public void Update() {
			Connector.Update();
			if (Status == ClientStatus.Disconnected) return;

			if (_lastLatencyRequest.AddSeconds(types.Latency.RelayRequestLatency.IntervalLatencyRequest) < DateTime.Now) {
				_lastLatencyRequest = DateTime.Now;
				RequestLatency().Forget();
			}
		}


		private ushort _nextState = ushort.MinValue + 1;

		public ushort NextState() {
			if (_nextState == ushort.MaxValue)
				_nextState = ushort.MinValue + 1;
			return _nextState++;
		}

		public async UniTask<(bool, ushort)> Emit(
			Buffer      data,
			RequestType type  = RequestType.None,
			ushort      state = ushort.MaxValue) {
			if (!Connector.IsConnected()) return (false, ushort.MaxValue);
			var buffer                          = new Buffer();
			if (state == ushort.MaxValue) state = NextState();
			buffer.Write((ushort)(data.length + 4));
			buffer.Write(state);
			buffer.Write(type);
			buffer.Write(data.ToBuffer());
			return (await Connector.Send(buffer), state);
		}

		private async UniTask<T> Request<T>(
			Buffer       request,
			RequestType  oType,
			ResponseType iType,
			ushort       state,
			byte         timeout = 5)
			where T : RelayResponse, new() {
			T res = null;
			var rec = new IConnector.OnReceived(
				buffer => {
					if (buffer.length < 5) return;
					buffer.Goto(0);
					var length        = buffer.ReadUShort();
					var responseState = buffer.ReadUShort();
					var responseType  = buffer.ReadByte();
					if (responseType != (byte)iType) return;
					if (state != ushort.MaxValue && responseState != state) return;
					var response = new T { ConnectionId = Id, State = state };
					res = response.FromBuffer(buffer.Clone(5, length)) ? response : null;
				}
			);
			Connector.OnReceivedEvent += rec;
			var t0 = DateTime.Now;

			var (ok, _) = await Emit(request, oType, state);
			if (!ok) {
				Connector.OnReceivedEvent -= rec;
				Logger.Log($"Request failed: {oType} {state}");
				return null;
			}

			var time = DateTime.Now;
			await UniTask.WaitUntil(() => (DateTime.Now - time).TotalSeconds > timeout || res != null);
			Connector.OnReceivedEvent -= rec;
			var t1 = DateTime.Now;
			Logger.Log($"Request {oType} {state} took {t1 - t0} ({(t1 - t0).TotalMilliseconds}ms) " + $"({(res != null ? "OK" : "Timeout")})");
			return res;
		}

		public async UniTask<types.Handshakes.RelayResponseHandshake> RequestHandshake()
			=> _lastHandshake = await Request<types.Handshakes.RelayResponseHandshake>(
				new types.Handshakes.RelayRequestHandshake {
					Engine          = EngineExtensions.CurrentEngine,
					Platform        = PlatformExtensions.CurrentPlatform,
					ProtocolVersion = ProtocolVersion
				}.ToBuffer(),
				RequestType.Handshake,
				ResponseType.Handshake,
				NextState()
			);

		public async UniTask<types.Latency.RelayResponseLatency> RequestLatency()
			=> _lastLatency = await Request<types.Latency.RelayResponseLatency>(
				new types.Latency.RelayRequestLatency { InitialTime = DateTime.UtcNow }.ToBuffer(),
				RequestType.Latency,
				ResponseType.Latency,
				NextState()
			);
	}
}