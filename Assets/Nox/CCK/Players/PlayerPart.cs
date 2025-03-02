using UnityEngine;
using Transform = Nox.CCK.Utils.Transform;

namespace Nox.CCK.Players
{
    public class PlayerPart
    {
        public readonly PlayerRig Rig;
        public readonly Transform Transform;
        
        public PlayerPart(PlayerRig rig, Transform transform)
        {
            Rig = rig;
            Transform = transform;
        }
    }
}