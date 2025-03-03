using Nox.CCK.Utils;
using UnityEngine.InputSystem;

namespace api.nox.keybinding
{
    public class KeyBinding : INoxObject
    {
        internal KeyBinding(string id, string category, InputAction action)
        {
            Id = id;
            Category = category;
            Action = action;
            IsOverridden = false;
        }

        [NoxPublic(NoxAccess.Read)] public readonly string Id;
        [NoxPublic(NoxAccess.Read)] public readonly string Category;
        [NoxPublic(NoxAccess.Read)] public readonly InputAction Action;
        [NoxPublic(NoxAccess.Read)] public bool IsOverridden;
    }
}