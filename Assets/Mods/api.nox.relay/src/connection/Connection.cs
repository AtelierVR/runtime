using System;
using System.Linq;
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
			return connection;
		}

		public readonly ushort     Id;
		public readonly IConnector Connector;

		private Connection(IConnector connector) {
			Id                        =  RelaySystem.Instance.NextId();
			Connector                 =  connector;
			Connector.OnReceivedEvent += OnReceived;
			RelaySystem.Instance.Connections.Add(this);
			RelaySystem.OnConnectionAdded.Invoke(this);
		}

		public async UniTask<bool> Connect(string address, ushort port)
			=> await Connector.Connect(address, port);

		private void OnReceived(Buffer buffer) {
			if (buffer.length < 5) return;
			buffer.Goto(0);
			var length = buffer.ReadUShort();
			var state  = buffer.ReadUShort();
			var type   = buffer.ReadEnum<ResponseType>();
			if (length < 5 || length > buffer.length) return;
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
					var disconnect = new types.Disconnect.RelayEventDisconnect();
					if (disconnect.FromBuffer(buffer.Clone(5, length))) {
						Logger.Log($"Disconnect {disconnect.Reason}");
						_lastHandshake = null;
						_lastLatency   = null;
						_lastSessions  = null;
						Connector.Close().Forget();
					} else {
						Logger.LogError($"Failed to parse disconnect");
					}

					break;
			}
		}

		private DateTime                                _lastLatencyRequest = DateTime.MinValue;
		private types.Handshakes.RelayResponseHandshake _lastHandshake;
		private types.Latency.RelayResponseLatency      _lastLatency;
		private types.Session.RelayResponseSessions     _lastSessions;

		public ClientStatus Status
			=> _lastHandshake?.Status ?? ClientStatus.Disconnected;

		public ushort ClientId
			=> _lastHandshake?.ClientId ?? ushort.MaxValue;

		public double Latency
			=> _lastLatency?.GetLatency().TotalMilliseconds ?? -1;

		public async UniTask Dispose() {
			if (Connector.IsConnected()) {
				await RequestDisconnect();
				await Connector.Close();
			}

			_lastHandshake = null;
			_lastLatency   = null;
			_lastSessions  = null;
			RelaySystem.Instance.Connections.Remove(this);
			RelaySystem.OnConnectionRemoved.Invoke(this);
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

		// ReSharper disable Unity.PerformanceAnalysis
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
					var responseType  = buffer.ReadEnum<ResponseType>();
					Logger.LogDebug($"Requested: {length} {responseType} {responseState} ");
					if (responseType != iType) return;
					if (state != ushort.MaxValue && responseState != state) return;
					var response = new T { ConnectionId = Id, State = state };
					res = response.FromBuffer(buffer.Clone(5, length)) ? response : null;
					if (res != null) return;
					Logger.LogError($"Requested: Failed to parse {responseType}");
				}
			);
			Connector.OnReceivedEvent += rec;
			var t0 = DateTime.Now;

			var (ok, _) = await Emit(request, oType, state);
			if (!ok) {
				Connector.OnReceivedEvent -= rec;
				Logger.Log($"Requested: failed {oType} {state}");
				return null;
			}

			var time = DateTime.Now;
			await UniTask.WaitUntil(() => (DateTime.Now - time).TotalSeconds > timeout || res != null);
			Connector.OnReceivedEvent -= rec;
			if (res != null) {
				res.Time = (t0, DateTime.Now);
				return res;
			}

			Logger.Log($"Requested: {oType} {state} timeout");
			return null;
		}

		public async UniTask<types.Handshakes.RelayResponseHandshake> RequestHandshake()
			=> _lastHandshake = await Request<types.Handshakes.RelayResponseHandshake>(
				new types.Handshakes.RelayRequestHandshake {
					ConnectionId    = Id,
					ProtocolVersion = ProtocolVersion,
					Engine          = EngineExtensions.CurrentEngine,
					Platform        = PlatformExtensions.CurrentPlatform
				}.ToBuffer(),
				RequestType.Handshake,
				ResponseType.Handshake,
				NextState()
			);

		public async UniTask<types.Latency.RelayResponseLatency> RequestLatency()
			=> _lastLatency = await Request<types.Latency.RelayResponseLatency>(
				new types.Latency.RelayRequestLatency {
					ConnectionId = Id,
					InitialTime  = DateTime.UtcNow
				}.ToBuffer(),
				RequestType.Latency,
				ResponseType.Latency,
				NextState()
			);

		public async UniTask<types.Session.RelayResponseSessions> RequestSessions(byte page)
			=> _lastSessions = await Request<types.Session.RelayResponseSessions>(
				new types.Session.RelayRequestSessions {
					ConnectionId = Id,
					Page         = page
				}.ToBuffer(),
				RequestType.Sessions,
				ResponseType.Sessions,
				NextState()
			);

		public async UniTask<types.Session.RelayResponseSessions> RequestSessions() {
			var all = await RequestSessions(0);
			if (all == null) return null;

			var l = all.Instances.ToList();
			for (byte i = 1; i < all.PageCount; i++) {
				var next = await RequestSessions(i);
				if (next == null) break;
				l.AddRange(next.Instances);
			}

			all.Instances = l.ToArray();
			return _lastSessions = all;
		}

		public async UniTask<types.Disconnect.RelayEventDisconnect> RequestDisconnect(string reason = null)
			=> await Request<types.Disconnect.RelayEventDisconnect>(
				new types.Disconnect.RelayRequestDisconnect {
					ConnectionId = Id,
					Reason       = reason
				}.ToBuffer(),
				RequestType.Disconnect,
				ResponseType.Disconnect,
				NextState()
			);
	}
}