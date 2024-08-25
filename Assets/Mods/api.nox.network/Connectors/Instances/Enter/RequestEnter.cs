
using api.nox.network.Instances.Base;
using api.nox.network.Utils;

namespace api.nox.network.Instances.Enter
{
    public class RequestEnter : InstanceRequest
    {
        public EnterFlags Flags;
        public string DisplayName;
        public string Password;

        public override Buffer ToBuffer()
        {
            var buffer = new Buffer();
            if (!string.IsNullOrEmpty(DisplayName))
                Flags |= EnterFlags.UsePseudonyme;
            else Flags &= ~EnterFlags.UsePseudonyme;
            if (!string.IsNullOrEmpty(Password))
                Flags |= EnterFlags.UsePassword;
            else Flags &= ~EnterFlags.UsePassword;
            buffer.Write((byte)Flags);
            if (Flags.HasFlag(EnterFlags.UsePseudonyme))
                buffer.Write(DisplayName);
            if (Flags.HasFlag(EnterFlags.UsePassword))
                buffer.Write(Password);
            return buffer;
        }
    }
}