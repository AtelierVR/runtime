
using api.nox.network.Instances.Base;
using api.nox.network.Utils;
using Nox.CCK.Mods;

namespace api.nox.network.Instances.Enter
{
    public class RequestEnter : InstanceRequest, ShareObject
    {
        public EnterFlags Flags;
        [ShareObjectExport] public string DisplayName;
        [ShareObjectExport] public string Password;

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

        [ShareObjectExport] public byte SharedFlags;

        public void BeforeExport()
        {
            SharedFlags = (byte)Flags;
        }

        public void AfterExport()
        {
            SharedFlags = 0;
        }
    }
}