using Nox.Network;
using Nox.Network.Instances;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Nox.Events;
using Nox.UI;
using Nox.Mods;
using System.Linq;
using UnityEngine.UIElements;
using System.IO;

namespace Nox
{
    public class Main : MonoBehaviour
    {
        public static Main Instance { get; private set; }

        public void Start()
        {
            EventEmitter.Clear();
            DontDestroyOnLoad(gameObject);
            Instance = this;
            Init().Forget();
        }

        public void Update()
        {
            ModManager.Update();
        }

        public void LateUpdate()
        {
            ModManager.LateUpdate();
        }

        public void FixedUpdate()
        {
            ModManager.FixedUpdate();
        }



        private static async UniTaskVoid Init()
        {
            Debug.Log("Initializing Nox...");
            ModManager.Init();
            var results = ModManager.LoadAllClientMods();
            if (results.Where(r => r.IsError).Count() > 0)
            {
                foreach (var result in results)
                    Debug.LogError($"Failed to load mod at {result.Path}: {result.Message}");
                var path = Path.Combine(CCK.Constants.GameAppDataPath, "error-" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".txt");
                File.WriteAllText(path, "Nox failed to load some mods.");
                foreach (var result in results)
                    File.AppendAllText(path, $"\nFailed to load mod at {result.Path}: {result.Message}");
                Debug.LogError($"Nox failed to load some mods. Check {path} for more information.");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit(1);
#endif
            }
            else Debug.Log("All mods loaded successfully");
        }
    }
}