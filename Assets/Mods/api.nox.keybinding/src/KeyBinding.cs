using Nox.CCK.Utils;
using UnityEngine.InputSystem;

namespace api.nox.game.keybindings
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

        public readonly string Id;
        public readonly string Category;
        public readonly InputAction Action;
        public bool IsOverridden;
    }
}