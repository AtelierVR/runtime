# if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nox.Mods
{
    public class ModManager
    {

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            // check for mods
        }

        [InitializeOnEnterPlayMode]
        public static void OnEnterPlayMode()
        {
            // check for mods
        }
#endif
    }
}