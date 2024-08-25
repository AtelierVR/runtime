using api.nox.network.Instances.Base;
using api.nox.network.Utils;

namespace api.nox.network.Instances.Quit
{
    public class EventQuit : InstanceResponse
    {
        public QuitType Type;
        public string Reason;

        public override bool FromBuffer(Buffer buffer)
        {
            Type = buffer.Read<QuitType>();
            if (buffer.Remaining < 1)
                Reason = buffer.ReadString();
            return true;
        }

        public override string ToString() => $"{GetType().Name}[Type={Type}, Reason={Reason}]";
    }
}