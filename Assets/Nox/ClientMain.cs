using UnityEngine;
using Nox.Events;
using Nox.Mods;
using System.Linq;
using System.IO;
using Logger = Nox.CCK.Logger;

namespace Nox
{
    public class ClientMain : MonoBehaviour
    {
        public static ClientMain Instance { get; private set; }

        public void Start()
        {
            EventEmitter.Clear();
            DontDestroyOnLoad(gameObject);
            Instance = this;
            Init();

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



        private static void Init()
        {
            Logger.Log("Initializing Nox...");
            ModManager.Init();
            var results = ModManager.LoadAllClientMods();
            if (results.Where(r => r.IsError).Count() > 0)
            {
                var path = Path.Combine(CCK.Constants.GameAppDataPath, "error-" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".txt");
                File.WriteAllText(path, "Nox failed to load some mods.");
                foreach (var result in results)
                    File.AppendAllText(path, $"\nFailed to load mod at {result.Path}: {result.Message}");
                Logger.LogError($"Nox failed to load some mods. Check {path} for more information.");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit(1);
#endif
            }
            else Logger.Log("All mods loaded successfully");
        }

        // on game quit
        public void OnApplicationQuit()
        {
            ModManager.OnApplicationQuit();
        }
    }
}