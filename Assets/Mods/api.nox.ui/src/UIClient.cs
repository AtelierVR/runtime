using Mods.api.nox.ui.menus;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using UnityEngine;
using Transform = UnityEngine.Transform;

namespace Mods.api.nox.ui
{
    public class UIClient : ClientModInitializer
    {
        [NoxPublic(NoxAccess.Method)]
        public PhysicalMenu SpawnPhysical(Vector3 position, Quaternion rotation)
            => PhysicalMenu.Create(position, rotation);
        
        [NoxPublic(NoxAccess.Method)]
        public ViewportMenu SpawnViewport(Transform parent)
            => ViewportMenu.Create(parent);
    }
}