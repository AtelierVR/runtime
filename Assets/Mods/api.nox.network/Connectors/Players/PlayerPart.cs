namespace api.nox.network.Players
{
    public class PlayerPart
    {
        public PlayerRig Rig;
        public Utils.Transform Transform;

        public override string ToString() => $"{GetType().Name}[rig={Rig}, transform={Transform}]";
    }
}