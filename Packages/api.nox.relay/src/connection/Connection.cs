using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.relay.connector;
using api.nox.relay.Instances;
using api.nox.relay.types;
using api.nox.relay.types.Instance;
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
			Id                        =  Main.Instance.NextId();
			Connector                 =  connector;
			Connector.OnReceivedEvent += OnReceived;
			Main.Instance.Connections.Add(this);
			Main.OnConnectionAdded.Invoke(this);
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
			
			switch (type)
			{
				case ResponseType.Enter:
				case ResponseType.Quit:
				case ResponseType.Join:
				case ResponseType.Leave:
				case ResponseType.Teleport:
				case ResponseType.Transform:
				case ResponseType.Custom:
				case ResponseType.AvatarChanged:
				case ResponseType.Traveling:
					var iid = buffer.ReadByte();
					var instance = Instances.FirstOrDefault(x => x.InternalId == iid);
					instance?.OnReceived(length, state, type, buffer.Clone(5, length));
					break;
				case ResponseType.Disconnect:
					var disconnect = new types.Disconnect.RelayEventDisconnect();
					if (disconnect.FromBuffer(buffer.Clone(5, length)))
					{
						Logger.Log($"Disconnect {disconnect.Reason}");
						_lastHandshake = null;
						_lastLatency = null;
						Connector.Close().Forget();
					}
					else
					{
						Logger.LogError($"Failed to parse disconnect");
					}

					break;
			}
		}

		private void HandleMultiPacketStart(Buffer buffer, ushort state, ushort length)
		{
			var sessionId = buffer.ReadUShort();
			var totalPackets = buffer.ReadUShort();
			var totalSize = (uint)buffer.ReadInt();
			var originalType = buffer.ReadEnum<ResponseType>();
			var originalUid = buffer.ReadUShort();

			ClientMultiPacketManager.StartSession(sessionId, totalPackets, totalSize, originalType, state);
			Logger.LogDebug($"Started receiving multipacket session {sessionId} with {totalPackets} packets, size {totalSize}");
		}

		private void HandleMultiPacketData(Buffer buffer, ushort state, ushort length)
		{
			var sessionId = buffer.ReadUShort();
			var packetIndex = buffer.ReadUShort();
			var dataLength = buffer.ReadUShort();
			var remainingLength = length - 9; // 5 (header) + 2 (sessionId) + 2 (packetIndex)
			var actualLength = Math.Min(dataLength, remainingLength);
			
			if (actualLength <= 0)
			{
				Logger.LogWarning($"Invalid data length for multipacket session {sessionId}, packet {packetIndex}");
				return;
			}

			var data = new byte[actualLength];
			for (int i = 0; i < actualLength; i++)
			{
				data[i] = buffer.ReadByte();
			}

			ClientMultiPacketManager.AddPacket(sessionId, packetIndex, data);
			Logger.LogDebug($"Received multipacket data {packetIndex} for session {sessionId}");
		}

		private void HandleMultiPacketEnd(Buffer buffer, ushort state, ushort length)
		{
			var sessionId = buffer.ReadUShort();
			var session = ClientMultiPacketManager.CompleteSession(sessionId);

			if (session == null)
			{
				Logger.LogWarning($"Failed to complete multipacket session {sessionId}");
				return;
			}

			var mergedData = session.GetMergedData();
			if (mergedData == null)
			{
				Logger.LogError($"Failed to merge data for session {sessionId}");
				return;
			}

			Logger.LogDebug($"Successfully merged {mergedData.Length} bytes for session {sessionId}");

			// Create a new buffer with the merged data and process it as the original type
			var mergedBuffer = new Buffer((ushort)(mergedData.Length + 5));
			mergedBuffer.Write((ushort)(mergedData.Length + 5));
			mergedBuffer.Write(session.OriginalState);
			mergedBuffer.Write(session.OriginalType);
			mergedBuffer.Write(mergedData);
			mergedBuffer.Goto(0);

			// Process the merged packet recursively
			OnReceived(mergedBuffer);
		}

		internal readonly List<RelayInstance> Instances = new();

		private DateTime                                _lastLatencyRequest = DateTime.MinValue;
		private types.Handshakes.RelayResponseHandshake _lastHandshake;
		private types.Latency.RelayResponseLatency      _lastLatency;

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
			Main.Instance.Connections.Remove(this);
			Main.OnConnectionRemoved.Invoke(this);
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
				_nextState = 0;
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

		internal async UniTask<T> Request<T>(
			RelayRequest request,
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

			var (ok, _) = await Emit(request.ToBuffer(), oType, state);
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
				},
				RequestType.Handshake,
				ResponseType.Handshake,
				NextState()
			);

		public async UniTask<types.Latency.RelayResponseLatency> RequestLatency()
			=> _lastLatency = await Request<types.Latency.RelayResponseLatency>(
				new types.Latency.RelayRequestLatency {
					ConnectionId = Id,
					InitialTime  = DateTime.UtcNow
				},
				RequestType.Latency,
				ResponseType.Latency,
				NextState()
			);

		public async UniTask<types.Session.RelayResponseSessions> RequestSessions(byte page) {
			var sessions = await Request<types.Session.RelayResponseSessions>(
				new types.Session.RelayRequestSessions {
					ConnectionId = Id,
					Page         = page
				},
				RequestType.Status,
				ResponseType.Status,
				NextState()
			);
			foreach (var instance in sessions.Instances)
				instance.Connection = this;
			return sessions;
		}

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
			return all;
		}

		public async UniTask<RelayInstance> RequestSession(uint mid) {
			byte          total, page = 0;
			RelayInstance instance    = null;

			do {
				var sessions = await RequestSessions(page++);
				if (sessions == null) return null;
				total = sessions.PageCount;
				foreach (var inst in sessions.Instances) {
					if (inst.MasterId != mid) continue;
					instance = inst;
					break;
				}
			} while (instance == null && page < total);

			return instance;
		}

		public async UniTask<types.Disconnect.RelayEventDisconnect> RequestDisconnect(string reason = null)
			=> await Request<types.Disconnect.RelayEventDisconnect>(
				new types.Disconnect.RelayRequestDisconnect {
					ConnectionId = Id,
					Reason       = reason
				},
				RequestType.Disconnect,
				ResponseType.Disconnect,
				NextState()
			);

		public async UniTask<types.Authentication.RelayResponseAuthentication> RequestAuthentication(types.Authentication.RelayRequestAuthentication request)
			=> await Request<types.Authentication.RelayResponseAuthentication>(
					request,
					RequestType.Authentification,
					ResponseType.Authentification,
					NextState()
				)
				?? types.Authentication.RelayResponseAuthentication.CreateUnknown(Id, "Unknown authentication request");
	}
}