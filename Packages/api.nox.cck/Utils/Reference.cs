using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nox.CCK.Utils
{
    public class Reference : MonoBehaviour
    {
        public string Key;
        public GameObject Refrence;
        public GameObject GetReference() => Refrence != null ? Refrence : gameObject;
        public static GameObject GetReference(string key, GameObject origin = null)
        {
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

        public static bool TryGetReference(string key, out GameObject reference, GameObject origin = null) 
            => reference = GetReference(key, origin);

        public static GameObject[] GetReferences(string key, GameObject origin = null)
        {
            var references = new HashSet<GameObject>();
            if (origin == null)
            {
                foreach (var reference in FindObjectsByType<Reference>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (reference.Key == key) references.Add(reference.GetReference());
            }
            else if (origin != null)
            {
                foreach (var reference in origin.GetComponents<Reference>())
                    if (reference.Key == key) references.Add(reference.GetReference());
                foreach (var reference in origin.GetComponentsInChildren<Reference>(true))
                    if (reference.Key == key) references.Add(reference.GetReference());
            }
            return references.ToArray();
        }
        
        public static T GetComponent<T>(string key, GameObject origin = null) where T : Component
            => TryGetReference(key, out var reference, origin) ? reference.GetComponent<T>() : null;
        
        public static T[] GetComponents<T>(string key, GameObject origin = null) where T : Component
            => GetReferences(key, origin)
                .Select(reference => reference.TryGetComponent(out T component) ? component : null)
                .Where(component => component != null)
                .ToArray();
    }
}
