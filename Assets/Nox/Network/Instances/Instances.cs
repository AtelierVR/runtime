using System.Collections.Generic;
using Nox.Network.Instances.Base;
using Nox.Network.Instances.Config;
using Nox.Network.Instances.Enter;
using Nox.Network.Instances.Quit;
using Nox.Network.Relays;
using Nox.Scripts;
using Cysharp.Threading.Tasks;
using Nox.Network.Instances.Transform;

// ReSharper disable All

namespace Nox.Network.Instances
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class Instance
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        public uint Id;
        public ushort InternalId;
        public InstanceFlags Flags;
        public string ServerAdress;
        public ushort RelayId;
        public Relay Relay => RelayManager.Get(RelayId);
        public ushort PlayerCount;
        public ushort MaxPlayerCount;

        public delegate void OnInstanceEvent(Buffer buffer);
        public event OnInstanceEvent OnInstanceEventEvent;
        public void OnInstanceEventInvoke(Buffer buffer) => OnInstanceEventEvent?.Invoke(buffer);

        public Instance() => InstanceManager.Set(this);

        public ResponseEnter LastEnter { get; private set; }
        public async UniTask<ResponseEnter> Enter(RequestEnter request)
        {
            request.RelayId = RelayId;
            request.InternalId = InternalId;
            var buffer = new Buffer();
            buffer.Write(InternalId);
            buffer.Write(request.ToBuffer());
            var uid = Relay.Send(buffer, RequestType.Enter);
            if (uid == ushort.MaxValue) return null;
            var response = await WaitForResponse<ResponseEnter>(uid, ResponseType.Enter);
            LastEnter = response;
            return response;
        }

        public async UniTask<EventQuit> Quit()
        {
            var buffer = new Buffer();
            buffer.Write(InternalId);
            buffer.Write(new RequestQuit().ToBuffer());
            var uid = Relay.Send(buffer, RequestType.Quit);
            if (uid == ushort.MaxValue) return null;
            return await WaitForResponse<EventQuit>(ushort.MaxValue, ResponseType.Quit);
        }

        public async UniTask<ResponseConfigWorldData> RequestConfigWorldData()
        {
            var buffer = new Buffer();
            buffer.Write(InternalId);
            buffer.Write(new RequestConfigWorldData().ToBuffer());
            var uid = Relay.Send(buffer, RequestType.Configuration);
            if (uid == ushort.MaxValue) return null;
            return await WaitForResponse<ResponseConfigWorldData>(uid, ResponseType.Configuration);
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

        private async UniTask<T> WaitForResponse<T>(ushort uid, ResponseType type, byte timeout = 5)
            where T : InstanceResponse, new()
        {
            T res = null;
            var rec = new IConnector.OnReceived((buffer) =>
            {
                if (buffer.length < 7) return;
                buffer.Goto(0);
                var length = buffer.ReadUShort();
                var ruid = buffer.ReadUShort();
                var rtype = buffer.ReadEnum<ResponseType>();
                var riid = buffer.ReadUShort();
                if (rtype != type || riid != InternalId) return;
                if (uid != ushort.MaxValue && ruid != uid) return;
                var rres = new T { RelayId = RelayId, UId = uid, InternalId = InternalId };
                res = rres.FromBuffer(buffer.Clone(5, length)) ? rres : null;
            });
            Relay.Connector.OnReceivedEvent += rec;
            var time = System.DateTime.Now;
            await UniTask.WaitUntil(() => (System.DateTime.Now - time).TotalSeconds > timeout || res != null);
            Relay.Connector.OnReceivedEvent -= rec;
            return res;
        }

        public override string ToString() => $"{GetType().Name}[InternalId={InternalId}, Flags={Flags}, PlayerCount={PlayerCount}/{MaxPlayerCount}]";

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var instance = (Instance)obj;
            return InternalId == instance.InternalId && RelayId == instance.RelayId;
        }
    }
}