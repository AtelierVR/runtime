using UnityEngine;

namespace Nox.CCK
{
    public class Reference : MonoBehaviour
    {
        public string Key;
        public GameObject Refrence;
        public GameObject GetReference() => Refrence != null ? Refrence : gameObject;
        public static GameObject GetReference(string key, GameObject origin = null)
        {
            Debug.Log("GetReference[" + key + "] from " + origin);
            if (origin == null)
            {
                foreach (var reference in FindObjectsByType<Reference>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (reference.Key == key) return reference.GetReference();
            }
            else if (origin != null)
            {
                foreach (var reference in origin.GetComponents<Reference>())
                    if (reference.Key == key) return reference.GetReference();
                foreach (var reference in origin.GetComponentsInChildren<Reference>(true))
                    if (reference.Key == key) return reference.GetReference();
            }
            return null;
        }
    }
}
