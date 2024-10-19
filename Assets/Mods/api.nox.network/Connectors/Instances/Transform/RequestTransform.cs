using api.nox.network.RelayInstances.Base;
using api.nox.network.Players;
using api.nox.network.Utils;
using Nox.CCK.Mods;

namespace api.nox.network.RelayInstances.Transform
{
    public class RequestTransform : InstanceRequest, ShareObject
    {
        public TransformType Type;
        public Utils.Transform Transform;
        public TransformFlags Flags;

        [ShareObjectImport] public string Path;
        [ShareObjectImport] public ushort PlayerId;
        public PlayerRig PlayerRig;
        [ShareObjectImport] public ushort ObjectId;

        public override Buffer ToBuffer()
        {
            var buffer = new Buffer();
            buffer.Write(Type);
            switch (Type)
            {
                case TransformType.ByPath:
                    buffer.Write(Path);
                    break;
                case TransformType.OnPlayer:
                    buffer.Write(PlayerId);
                    buffer.Write(PlayerRig);
                    break;
                case TransformType.OnObject:
                    buffer.Write(ObjectId);
                    break;
                default:
                    return null;
            }
            if (Flags.HasFlag(TransformFlags.Position))
                buffer.Write(Transform.position);
            if (Flags.HasFlag(TransformFlags.Rotation))
                buffer.Write(Transform.rotation);
            if (Flags.HasFlag(TransformFlags.Scale))
                buffer.Write(Transform.scale);
            if (Flags.HasFlag(TransformFlags.Velocity))
                buffer.Write(Transform.velocity);
            if (Flags.HasFlag(TransformFlags.AngularVelocity))
                buffer.Write(Transform.angularVelocity);
            return buffer;
        }

        public void Set(ushort playerId, PlayerPart part)
        {
            Type = TransformType.OnPlayer;
            PlayerId = playerId;
            PlayerRig = part.Rig;
            Transform = part.Transform;
        }

        public void Set(Object obj)
        {
            Type = TransformType.OnObject;
            ObjectId = obj.Id;
            Transform = obj.GetObjectTransform();
        }

        public void Set(UnityEngine.Transform transform)
        {
            Type = TransformType.ByPath;
            Path = transform.name;
            var tr = transform.parent;
            while (tr != null)
            {
                Path = tr.name + "/" + Path;
                tr = tr.parent;
            }
            Transform = new Utils.Transform(transform);
        }


        [ShareObjectImport] public byte SharedType;
        [ShareObjectImport] public ShareObject SharedTransform;
        [ShareObjectImport] public byte SharedFlags;
        [ShareObjectImport] public ushort SharedPlayerRig;

        public void AfterImport()
        {
            Type = (TransformType)SharedType;
            Transform = SharedTransform?.Convert<Utils.Transform>();
            Flags = (TransformFlags)SharedFlags;
            PlayerRig = (PlayerRig)SharedPlayerRig;
            SharedType = 0;
            SharedTransform = null;
            SharedFlags = 0;
            SharedPlayerRig = 0;
        }
    }
}