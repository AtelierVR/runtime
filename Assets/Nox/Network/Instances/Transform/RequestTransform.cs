using Nox.Network.Instances.Base;
using Nox.Network.Players;
using Nox.Scripts;

namespace Nox.Network.Instances.Transform
{
    public class RequestTransform : InstanceRequest
    {
        public TransformType Type;
        public Scripts.Transform Transform;
        public TransformFlags Flags;

        public string Path;
        public ushort PlayerId;
        public PlayerRig PlayerRig;
        public ushort ObjectId;

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
            Transform = new Scripts.Transform(transform);
        }
    }
}