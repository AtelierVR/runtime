using api.nox.network.Instances.Base;
using api.nox.network.Utils;
using Nox.CCK.Mods;

namespace api.nox.network.Instances.Quit
{
    public class EventQuit : InstanceResponse, ShareObject
    {
        public QuitType Type;
        [ShareObjectExport] public string Reason;
        [ShareObjectExport] public byte SharedType;

        public override bool FromBuffer(Buffer buffer)
        {
            Type = buffer.Read<QuitType>();
            if (buffer.Remaining < 1)
                Reason = buffer.ReadString();
            return true;
        }

        public override string ToString() => $"{GetType().Name}[Type={Type}, Reason={Reason}]";

        public void BeforeExport()
        {
            SharedType = (byte)Type;
        }

        public void AfterExport()
        {
            SharedType = 0;
        }
    }
}