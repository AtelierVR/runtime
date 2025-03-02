using System;
using System.Collections.Generic;
using System.Linq;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif // UNITY_EDITOR

namespace Nox.CCK.Worlds
{
    public class JintScript : MonoBehaviour
    {
        [SerializeField] private string data_SerializedScript;

        [SerializeField] private Dictionary<string, string> data_Data = new();

        private JsValue SetData(JsValue[] args)
        {
            if (args.Length < 2)
                return null;
            if (!args[0].IsString() || args[1].IsString())
                return null;
            return SetData(args[0].AsString(), args[1].AsString());
        }

        private bool SetData(string key, string value)
        {
            if (data_Data.Count > 100)
                return false;
            data_Data[key] = value;
            return true;
        }

        private JsValue GetData(JsValue[] args)
        {
            if (args.Length < 1 || !args[0].IsString())
                return JsValue.Undefined;
            return GetData(args[0].AsString());
        }

        private string GetData(string key)
            => data_Data.GetValueOrDefault(key);

        private JsValue GetDataKeys(JsValue[] args)
            => new JsArray(Engine, GetDataKeys().Select(key => (JsValue)key).ToArray());

        private string[] GetDataKeys()
        {
            if (data_Data == null)
                return Array.Empty<string>();
            var keys = new string[data_Data.Count];
            data_Data.Keys.CopyTo(keys, 0);
            return keys;
        }


        private Engine Engine { get; set; }

        public ObjectInstance GetExports()
            => Engine.GetValue("exports").AsObject();

        public T GetExport<T>(string key) where T : ObjectInstance
            => GetExports().Get(key).As<T>();

        public void SetExports(ObjectInstance exports)
            => Engine.SetValue("exports", exports);

        public void SetExport<T>(string key, T value) where T : ObjectInstance
            => GetExports().Set(key, value);

        public void Awake()
        {
            if (Engine == null)
                Prepare();
            try
            {
                Engine.Invoke("onAwake");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error executing onAwake function: {e.Message}");
            }
        }

        public JintConstraint Constraint;

        public void Prepare()
        {
            Engine = new Engine(ctx =>
            {
                ctx.LimitMemory(4_194_304);
                ctx.LimitRecursion(1024);
                ctx.Constraint(Constraint);
            });

            Engine.SetValue("log", new Action<object>(Logger.Log));
            Engine.SetValue("warn", new Action<object>(Logger.LogWarning));
            Engine.SetValue("error", new Action<object>(Logger.LogError));
            Engine.SetValue("gameObject", gameObject);
            Engine.SetValue("transform", transform);

            Engine.SetValue("Vector3", new Func<float, float, float, Vector3>((x, y, z) => new Vector3(x, y, z)));
            Engine.SetValue("Vector2", new Func<float, float, Vector2>((x, y) => new Vector2(x, y)));
            Engine.SetValue("Quaternion",
                new Func<float, float, float, float, Quaternion>((x, y, z, w) => new Quaternion(x, y, z, w)));
            Engine.SetValue("Color",
                new Func<float, float, float, float, Color>((r, g, b, a) => new Color(r, g, b, a)));
            

            Engine.AddModule("api", builder
                => builder
                    .ExportFunction("setData", SetData)
                    .ExportFunction("getData", GetData)
                    .ExportFunction("getDataKeys", GetDataKeys)
            );
            
            try
            {
                Engine.Execute(GetScriptCode());
                Engine.Invoke("onPrepare");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error executing onPrepare function: {e.Message}");
                Engine = null;
            }
        }

        public void Start()
        {
            if (Engine == null) return;
            try
            {
                Engine.Invoke("onStart");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error executing onStart function: {e.Message}");
            }
        }

        public void Update()
        {
            if (Engine == null) return;
            try
            {
                Engine.Invoke("onUpdate");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error executing onUpdate function: {e.Message}");
            }
        }

        public void FixedUpdate()
        {
            if (Engine == null) return;
            try
            {
                Engine.Invoke("onFixedUpdate");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error executing onFixedUpdate function: {e.Message}");
            }
        }

        public void LateUpdate()
        {
            if (Engine == null) return;
            try
            {
                Engine.Invoke("onLateUpdate");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error executing onLateUpdate function: {e.Message}");
            }
        }

        public void OnDestroy()
        {
            if (Engine == null) return;
            try
            {
                Engine.Invoke("onDestroy");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error executing onDestroy function: {e.Message}");
            }
        }

        public void OnEnable()
        {
            if (Engine == null) return;
            try
            {
                Engine.Invoke("onEnable");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error executing onEnable function: {e.Message}");
            }
        }

        public void OnDisable()
        {
            if (Engine == null) return;
            try
            {
                Engine.Invoke("onDisable");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error executing onDisable function: {e.Message}");
            }
        }

        void Dispose()
        {
            if (Engine == null) return;
            try
            {
                Engine.Invoke("onDispose");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error executing onDispose function: {e.Message}");
            }

            Engine.Dispose();
            Engine = null;
        }

#if UNITY_EDITOR
        public bool IsCompiled;
        public string scriptPath;

        public void Compile()
        {
            data_SerializedScript = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    GetScriptCode()
                )
            );
            EditorUtility.SetDirty(this);
            IsCompiled = true;
        }

        public string GetScriptCode()
            => IsCompiled
                ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(data_SerializedScript))
                : ReadScriptFile();

        public string ReadScriptFile()
            => File.Exists(scriptPath)
                ? File.ReadAllText(scriptPath)
                : string.Empty;

        public void OnValidate()
        {
            if (IsCompiled)
                return;
            Dispose();
            Prepare();
        }

        [InitializeOnLoadMethod]
        public static void OnStartInEditor()
        {
            EditorApplication.update += OnUpdateInEditor;
        }

        private DateTimeOffset lastFileUpdate = DateTimeOffset.MinValue;
        private static DateTimeOffset _lastUpdate = DateTimeOffset.MinValue;

        private static void OnUpdateInEditor()
        {
            if (DateTimeOffset.Now - _lastUpdate < TimeSpan.FromSeconds(1)) return;
            _lastUpdate = DateTimeOffset.Now;
            var scripts = new List<JintScript>();
            for (var sceneIndex = 0; sceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCount; sceneIndex++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(sceneIndex);
                foreach (var rootGameObject in scene.GetRootGameObjects())
                    scripts.AddRange(rootGameObject.GetComponentsInChildren<JintScript>());
            }


            foreach (var script in scripts)
            {
                if (script.IsCompiled || string.IsNullOrEmpty(script.scriptPath) || !File.Exists(script.scriptPath))
                    continue;
                var lastWriteTime = File.GetLastWriteTime(script.scriptPath);
                if (lastWriteTime <= script.lastFileUpdate) continue;
                script.lastFileUpdate = lastWriteTime;
                script.OnValidate();
            }
        }
#else
        public string GetScriptCode()
            => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(data_SerializedScript));
#endif // UNITY_EDITOR
    }
}