using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyRelayRequestTransform : ShareObject
    {
        [ShareObjectExport] public string Path;
        [ShareObjectExport] public ushort PlayerId;
        [ShareObjectExport] public ushort ObjectId;
        [ShareObjectExport] public byte SharedType;
        [ShareObjectExport] public ShareObject SharedTransform;
        [ShareObjectExport] public byte SharedFlags;
        [ShareObjectExport] public ushort SharedPlayerRig;
        
        public SimplyTransformType Type;
        public SimplyTransform Transform;
        public SimplyTransformFlags Flags;
        public SimplyPlayerRig PlayerRig;

        public void BeforeExport()
        {
            SharedType = (byte)Type;
            SharedTransform = Transform;
            SharedFlags = (byte)Flags;
            SharedPlayerRig = (ushort)PlayerRig;
        }

        public void AfterExport()
        {
            SharedType = 0;
            SharedTransform = null;
            SharedFlags = 0;
            SharedPlayerRig = 0;
        }
    }
}