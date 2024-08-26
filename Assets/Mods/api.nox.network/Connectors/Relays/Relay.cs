using System;
using System.Collections.Generic;
using System.Net;
using api.nox.network.Instances;
using api.nox.network.Relays.Base;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using UnityEngine;
using Buffer = api.nox.network.Utils.Buffer;
using Random = UnityEngine.Random;

namespace api.nox.network.Relays
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class Relay : ShareObject, IDisposable
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        public ushort Id;
        public IConnector Connector;
        public ushort ClientId;
        public IPEndPoint EndPoint;
        public List<Instances.Instance> Instances => InstanceManager.Get(Id);

        public string Address => EndPoint != null ? $"{EndPoint.Address}:{EndPoint.Port}" : null;
        public object UserData;

        public ClientStatus Status => LastHandshake?.ClientStatus ?? ClientStatus.Disconnected;


        public Relay(IConnector connector)
        {
            Id = RelayManager.NextId();
            RelayManager.Set(this);
            Connector = connector;
            Connector.OnReceivedEvent += OnReceived;
        }

        public delegate void OnRelayEvent(Buffer buffer);
        public event OnRelayEvent OnRelayEventEvent;
        public delegate void OnInstanceEvent(Buffer buffer);
        public event OnInstanceEvent OnInstanceEventEvent;

        void OnReceived(Buffer buffer)
        {
            if (buffer.length < 5) return;
            buffer.Goto(0);
            var length = buffer.ReadUShort();
            var state = buffer.ReadUShort();
            var type = (ResponseType)buffer.ReadByte();
            if (length < 5 || length > buffer.length) return;
            OnRelayEventEvent?.Invoke(buffer);
            if (length >= 6)
                switch (type)
                {
                    case ResponseType.Enter:
                    case ResponseType.Quit:
                    case ResponseType.Join:
                    case ResponseType.Leave:
                    case ResponseType.Teleport:
                    case ResponseType.Transform:
                    case ResponseType.CustomDataPacket:
                        var iid = buffer.ReadUShort();
                        var instance = InstanceManager.Get(iid, Id);
                        instance?.OnInstanceEventInvoke(buffer.Clone(5, (ushort)(length - 5)));
                        break;
                    case ResponseType.Disconnect:
                        var message = buffer.ReadString();
                        Debug.Log($"Received disconnect message: {message}");
                        LastHandshake = null;
                        break;
                }
        }

        public DateTime _lastLatencyRequest = DateTime.MinValue;

        public void Update()
        {
            Connector.Update();
            if (Status == ClientStatus.Disconnected) return;

            if (_lastLatencyRequest.AddSeconds(Nox.CCK.Constants.IntervalLantencyRequest) < DateTime.Now)
            {
                _lastLatencyRequest = DateTime.Now;
                UniTask.Create(async () => await RequestLatency()).Forget();
            }
        }

        public ushort Send(Buffer data, RequestType type = RequestType.None)
        {
            if (!Connector.IsConnected()) return ushort.MaxValue;
            var buffer = new Buffer();
            var state = (ushort)Random.Range(ushort.MinValue, ushort.MaxValue);
            buffer.Write((ushort)(data.length + 4));
            buffer.Write(state);
            buffer.Write(type);
            buffer.Write(data.ToBuffer());
            return Connector.Send(buffer) ? state : ushort.MaxValue;
        }

        private async UniTask<T> WaitForResponse<T>(ushort uid, ResponseType type, byte timeout = 5)
            where T : RelayResponse, new()
        {
            T res = null;
            var rec = new IConnector.OnReceived((buffer) =>
            {
                if (buffer.length < 5) return;
                buffer.Goto(0);
                var length = buffer.ReadUShort();
                var ruid = buffer.ReadUShort();
                var rtype = buffer.ReadByte();
                if (rtype != (byte)type) return;
                if (uid != ushort.MaxValue && ruid != uid) return;
                var rres = new T { RelayId = Id, UId = uid };
                res = rres.FromBuffer(buffer.Clone(5, length)) ? rres : null;
            });
            Connector.OnReceivedEvent += rec;
            var time = DateTime.Now;
            await UniTask.WaitUntil(() => (DateTime.Now - time).TotalSeconds > timeout || res != null);
            Connector.OnReceivedEvent -= rec;
            return res;
        }


        public override string ToString() =>
            $"{GetType().Name}[Id={Id}, Status={Status}, ClientId={ClientId}, EndPoint={EndPoint}]";

        // Request
        public bool SendDisconnect(string message = null)
        {
            var disconnect = new Disconnect.RequestDiscnonnect() { Reason = message };
            var uid = Send(disconnect.ToBuffer(), RequestType.Disconnect);
            if (uid == ushort.MaxValue) return false;
            Connector.Close();
            return true;
        }

        public Handshakes.ResponseHandshake LastHandshake;

        public async UniTask<Handshakes.ResponseHandshake> RequestHandshake()
        {
            var request = new Handshakes.RequestHandshake()
            {
                Engine = Nox.CCK.Constants.CurrentEngine,
                Platform = Nox.CCK.Constants.CurrentPlatform,
                ProtocolVersion = Nox.CCK.Constants.ProtocolVersion,
            };
            var uid = Send(request.ToBuffer(), RequestType.Handshake);
            if (uid == ushort.MaxValue) return null;
            var response = await WaitForResponse<Handshakes.ResponseHandshake>(uid, ResponseType.Handshake);
            LastHandshake = response;

            return response;
        }

        public Latency.ResponseLatency LastLatency;

        public async UniTask<Latency.ResponseLatency> RequestLatency()
        {
            var request = new Latency.RequestLatency { InitialTime = DateTime.Now };
            var uid = Send(request.ToBuffer(), RequestType.Latency);
            if (uid == ushort.MaxValue) return null;
            var response = await WaitForResponse<Latency.ResponseLatency>(uid, ResponseType.Latency);
            LastLatency = response;
            return response;
        }

        public Status.ResponseStatus LastStatus;

        public async UniTask<Status.ResponseStatus> RequestStatus()
        {
            var all = await RequestStatus(0);
            if (all == null) return null;
            for (byte i = 1; i < all.PageCount; i++)
            {
                var next = await RequestStatus(i);
                if (next == null) break;
                all.Instances.AddRange(next.Instances);
            }
            LastStatus = all;
            return all;
        }

        public async UniTask<Status.ResponseStatus> RequestStatus(byte page)
        {
            var request = new Status.RequestStatus { Page = page };
            var uid = Send(request.ToBuffer(), RequestType.Status);
            if (uid == ushort.MaxValue) return null;
            var response = await WaitForResponse<Status.ResponseStatus>(uid, ResponseType.Status);
            LastStatus = response;
            return response;
        }


        public Auth.ResponseAuth LastAuth;

        public async UniTask<Auth.ResponseAuth> RequestAuthentification(Auth.RequestAuth request)
        {
            var uid = Send(request.ToBuffer(), RequestType.Authentification);
            if (uid == ushort.MaxValue) return null;
            var response = await WaitForResponse<Auth.ResponseAuth>(uid, ResponseType.Authentification, 15);
            LastAuth = response;
            return response;
        }

        public bool Connect(string address, ushort port)
        {
            if (!Connector.Connect(address, port)) return false;
            EndPoint = Connector.Remote();
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var relay = (Relay)obj;
            return Id == relay.Id;
        }

        public void Dispose()
        {
            if (Status != ClientStatus.Disconnected)
                SendDisconnect();
            Connector.Close();
            RelayManager.Remove(this);
        }



        [ShareObjectExport] public Func<ushort> SharedGetId;
        [ShareObjectExport] public Func<ushort> SharedGetClientId;
        [ShareObjectExport] public Func<IPEndPoint> SharedGetEndPoint;
        [ShareObjectExport] public Func<object> SharedGetUserData;
        [ShareObjectExport] public Func<byte, UniTask<ShareObject>> SharedRequestStatus;
        [ShareObjectExport] public Func<UniTask<ShareObject>> SharedRequestAllStatus;
        [ShareObjectExport] public Func<UniTask<ShareObject>> SharedRequestLatency;

        public void BeforeExport()
        {
            SharedGetId = () => Id;
            SharedGetClientId = () => ClientId;
            SharedGetEndPoint = () => EndPoint;
            SharedGetUserData = () => UserData;
            SharedRequestStatus = async (page) => await RequestStatus(page);
            SharedRequestAllStatus = async () => await RequestStatus();
            SharedRequestLatency = async () => await RequestLatency();
        }

        public void AfterExport()
        {
            SharedGetId = null;
            SharedGetClientId = null;
            SharedGetEndPoint = null;
            SharedGetUserData = null;
            SharedRequestStatus = null;
            SharedRequestAllStatus = null;
            SharedRequestLatency = null;
        }

    }
}