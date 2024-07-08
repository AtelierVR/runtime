using UnityEngine;
using UnityEngine.InputSystem;

namespace Nox.CCK {
    public class Binding : Reference {
        public InputActionReference Action;
        public static InputActionReference GetBinding(string key, GameObject origin = null) => GetReference(key, origin)?.GetComponent<Binding>()?.Action;
    }
}