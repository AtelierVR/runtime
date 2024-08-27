using api.nox.network.Instances.Config;
using api.nox.network.Instances.Enter;
using api.nox.network.Instances.Quit;
using api.nox.network.Relays;
using Cysharp.Threading.Tasks;
using api.nox.network.Instances.Transform;
using api.nox.network.Utils;
using api.nox.network.Instances.Base;
using Nox.CCK.Mods;
using System;
using Buffer = api.nox.network.Utils.Buffer;

// ReSharper disable All

namespace api.nox.network.Instances
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class Instance : ShareObject
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        [ShareObjectExport] public uint Id;
        [ShareObjectExport] public ushort InternalId;
        [ShareObjectExport] public string ServerAdress;
        [ShareObjectExport] public ushort RelayId;
        [ShareObjectExport] public ushort PlayerCount;
        [ShareObjectExport] public ushort MaxPlayerCount;
        public InstanceFlags Flags;
        public Relay Relay => RelayManager.Get(RelayId);

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

        public void Dispose()
        {
            InstanceManager.Remove(this);
            OnInstanceEventEvent = null;
        }


        [ShareObjectExport] public uint SharedFlags;
        [ShareObjectExport] public Func<ShareObject> SharedGetRelay;
        [ShareObjectExport] public Func<ShareObject, UniTask<ShareObject>> SharedEnter;
        [ShareObjectExport] public Func<UniTask<ShareObject>> SharedQuit;
        [ShareObjectExport] public Func<UniTask<ShareObject>> SharedRequestConfigWorldData;
        [ShareObjectExport] public Func<bool> SharedSendConfigWorldLoaded;
        [ShareObjectExport] public Func<bool> SharedSendConfigReady;
        [ShareObjectExport] public Func<ShareObject, bool> SharedSendTransform;
        public void BeforeExport()
        {
            SharedFlags = (uint)Flags;
            SharedGetRelay = () => Relay;
            SharedEnter = async (obj) => await Enter(obj.Convert<RequestEnter>());
            SharedQuit = async () => await Quit();
            SharedRequestConfigWorldData = async () => await RequestConfigWorldData();
            SharedSendConfigWorldLoaded = () => SendConfigWorldLoaded();
            SharedSendConfigReady = () => SendConfigReady();
            SharedSendTransform = (obj) => SendTransform(obj.Convert<RequestTransform>());
        }

        public void AfterExport()
        {
            SharedFlags = 0;
            SharedGetRelay = null;
            SharedEnter = null;
            SharedQuit = null;
            SharedRequestConfigWorldData = null;
            SharedSendConfigWorldLoaded = null;
            SharedSendConfigReady = null;
        }
    }
}