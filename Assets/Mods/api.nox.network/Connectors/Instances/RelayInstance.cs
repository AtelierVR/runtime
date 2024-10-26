using api.nox.network.RelayInstances.Config;
using api.nox.network.RelayInstances.Enter;
using api.nox.network.RelayInstances.Quit;
using api.nox.network.Relays;
using Cysharp.Threading.Tasks;
using api.nox.network.RelayInstances.Transform;
using api.nox.network.RelayInstances.Base;
using System;
using Buffer = api.nox.network.Utils.Buffer;
using UnityEngine;
using System.Threading;
using Logger = Nox.CCK.Logger;
using System.Globalization;

// ReSharper disable All

namespace api.nox.network.RelayInstances
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class RelayInstance
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        public uint Id;
        public ushort InternalId;
        public string ServerAdress;
        public ushort RelayId;
        public ushort PlayerCount;
        public ushort MaxPlayerCount;
        public InstanceFlags Flags;
        public Relay Relay => RelayManager.Get(RelayId);

        public delegate void OnInstanceEvent(Buffer buffer);
        public event OnInstanceEvent OnInstanceEventEvent;
        public void OnInstanceEventInvoke(Buffer buffer) => OnInstanceEventEvent?.Invoke(buffer);

        public RelayInstance() => RelayInstanceManager.Set(this);

        public async UniTask<ResponseEnter> Enter(RequestEnter request)
        {
            request.RelayId = RelayId;
            request.InternalId = InternalId;
            var buffer = new Buffer();
            buffer.Write(InternalId);
            buffer.Write(request.ToBuffer());
            var uid = Relay.NextState();
            var source = new CancellationTokenSource();
            var wait = WaitForResponse<ResponseEnter>(uid, ResponseType.Enter, timeout: 20, cancellationToken: source.Token);
            uid = Relay.Send(buffer, RequestType.Enter, uid);
            if (uid == ushort.MaxValue)
            {
                source.Cancel();
                return null;
            }
            var response = await wait;
            return response;
        }

        public async UniTask<EventQuit> Quit(QuitType Type, string reason = null)
            => await Quit(new RequestQuit { Type = Type, Reason = reason });

        public async UniTask<EventQuit> Quit(RequestQuit request)
        {
            var buffer = new Buffer();
            buffer.Write(InternalId);
            buffer.Write(request.ToBuffer());
            var uid = Relay.NextState();
            var source = new CancellationTokenSource();
            var wait = WaitForResponse<EventQuit>(uid, ResponseType.Quit, timeout: 20, cancellationToken: source.Token);
            uid = Relay.Send(buffer, RequestType.Quit, uid);
            if (uid == ushort.MaxValue)
            {
                source.Cancel();
                return null;
            }
            var response = await wait;
            return response;
        }

        public async UniTask<ResponseConfigWorldData> RequestConfigWorldData()
        {
            var buffer = new Buffer();
            buffer.Write(InternalId);
            buffer.Write(new RequestConfigWorldData().ToBuffer());
            var uid = Relay.NextState();
            var source = new CancellationTokenSource();
            var wait = WaitForResponse<ResponseConfigWorldData>(uid, ResponseType.Configuration, timeout: 20, cancellationToken: source.Token);
            uid = Relay.Send(buffer, RequestType.Configuration, uid);
            if (uid == ushort.MaxValue)
            {
                source.Cancel();
                return null;
            }
            var response = await wait;
            return response;
        }

        public bool SendConfigWorldLoaded()
        {
            var buffer = new Buffer();
            buffer.Write(InternalId);
            buffer.Write(new SendConfigWorldLoaded().ToBuffer());
            var uid = Relay.Send(buffer, RequestType.Configuration);
            return uid != ushort.MaxValue;
        }

        public bool SendConfigReady()
        {
            var buffer = new Buffer();
            buffer.Write(InternalId);
            buffer.Write(new SendConfigReady().ToBuffer());
            var uid = Relay.Send(buffer, RequestType.Configuration);
            return uid != ushort.MaxValue;
        }

        public bool SendTransform(RequestTransform request)
        {
            var buffer = new Buffer();
            buffer.Write(InternalId);
            buffer.Write(request.ToBuffer());
            var uid = Relay.Send(buffer, RequestType.Transform);
            return uid != ushort.MaxValue;
        }


        public void Update()
        {
        }

        private async UniTask<T> WaitForResponse<T>(ushort uid, ResponseType type, byte timeout = 5, CancellationToken cancellationToken = default)
            where T : InstanceResponse, new()
        {
            var t0 = DateTime.Now;
            T res = null;
            var rec = new IConnector.OnReceived((buffer) =>
            {
                if (buffer.length < 7) return;
                buffer.Goto(0);
                var length = buffer.ReadUShort();
                var ruid = buffer.ReadUShort();
                var rtype = buffer.ReadEnum<ResponseType>();
                var riid = buffer.ReadUShort();
                if (rtype != type || riid != InternalId)
                    return;
                if (uid != ushort.MaxValue && ruid != uid)
                    return;
                var rres = new T { RelayId = RelayId, UId = uid, InternalId = InternalId };
                res = rres.FromBuffer(buffer.Clone(5, length)) ? rres : null;
            });
            Relay.Connector.OnReceivedEvent += rec;
            var time = DateTime.Now;
            await UniTask.WaitUntil(() => (DateTime.Now - time).TotalSeconds > timeout || res != null || cancellationToken.IsCancellationRequested);
            Relay.Connector.OnReceivedEvent -= rec;
            var t1 = DateTime.Now;
            Logger.Log($"WaitForResponse: {uid} {type} {(t1 - t0).TotalMilliseconds.ToString("0.000", CultureInfo.InvariantCulture)}ms");
            return res;
        }

        public override string ToString() => $"{GetType().Name}[InternalId={InternalId}, Flags={Flags}, PlayerCount={PlayerCount}/{MaxPlayerCount}]";

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var instance = (RelayInstance)obj;
            return InternalId == instance.InternalId && RelayId == instance.RelayId;
        }

        public void Dispose()
        {
            RelayInstanceManager.Remove(this);
            OnInstanceEventEvent = null;
        }
    }
}