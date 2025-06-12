// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Jint;
// using Jint.Native;
// using Jint.Native.Object;
// using Jint.Runtime.Interop;
// using Nox.CCK.Build;
// using UnityEngine;
// using UnityEngine.Serialization;
// using Logger = Nox.CCK.Utils.Logger;
// using LogType = Nox.CCK.Utils.LogType;
//
// #if UNITY_EDITOR
// using System.IO;
// using UnityEditor;
// using UnityEngine.Events;
// #endif // UNITY_EDITOR
//
// namespace Nox.CCK.Worlds {
// 	[Serializable]
// 	public struct JintDataValue {
// 		public string key;
// 		public string value;
// 	}
//
// 	public class JintScript : MonoBehaviour, ICompilable {
// 		[SerializeField] public List<JintDataValue> data = new();
//
// 		private JsValue SetData(JsValue[] args) {
// 			if (args.Length < 2)
// 				return null;
// 			if (!args[0].IsString() || args[1].IsString())
// 				return null;
// 			return SetData(args[0].AsString(), args[1].AsString());
// 		}
//
// 		public bool SetData(string key, string value) {
// 			if (dataData.Count > 100)
// 				return false;
// 			var index = dataData.FindIndex(x => x.key == key);
// 			if (index == -1)
// 				dataData.Add(new JintDataValue { key = key, value = value });
// 			else dataData[index] = new JintDataValue { key = key, value = value };
// 			return true;
// 		}
//
// 		private JsValue GetData(JsValue[] args) {
// 			if (args.Length < 1 || !args[0].IsString())
// 				return JsValue.Undefined;
// 			return GetData(args[0].AsString());
// 		}
//
// 		private string GetData(string key)
// 			=> dataData.Find(x => x.key == key).value;
//
// 		private JsValue GetDataKeys(JsValue[] args)
// 			=> new JsArray(Engine, GetDataKeys().Select(key => (JsValue)key).ToArray());
//
// 		public string[] GetDataKeys() {
// 			if (dataData == null)
// 				return Array.Empty<string>();
// 			var keys = new string[dataData.Count];
// 			for (var i = 0; i < dataData.Count; i++)
// 				keys[i] = dataData[i].key;
// 			return keys;
// 		}
//
//
// 		private Engine Engine { get; set; }
//
// 		public void Awake() {
// 			if (Engine == null)
// 				Prepare();
// 			try {
// 				Engine.Invoke("onAwake");
// 			} catch (Exception e) {
// 				Logger.LogError($"Error executing onAwake function: {e.Message}");
// 			}
// 		}
//
// 		private readonly JintConstraint _constraint;
// 		public           ObjectInstance ExecutionContext;
//
// 		#if UNITY_EDITOR
// 		public class LogData {
// 			public LogType  Type;
// 			public string   Message;
// 			public DateTime Time;
// 		}
//
// 		public List<LogData> LogList = new();
// 		public UnityEvent    onLog   = new();
// 		#endif
//
// 		private void Log(LogType type, JsValue[] args) {
// 			var parsed  = string.Join(" ", args.Select(x => x.ToString()));
// 			var message = $"Jint-{GetInstanceID()}: {parsed}";
// 			switch (type) {
// 				case LogType.Log:
// 					Logger.Log(message, this);
// 					break;
// 				case LogType.Warning:
// 					Logger.LogWarning(message, this);
// 					break;
// 				case LogType.Error:
// 					Logger.LogError(message, this);
// 					break;
// 				default:
// 					Logger.LogDebug(message, this);
// 					break;
// 			}
//
// 			#if UNITY_EDITOR
// 			LogList ??= new List<LogData>();
// 			LogList.Add(
// 				new LogData {
// 					Type    = type,
// 					Message = parsed,
// 					Time    = DateTime.Now
// 				}
// 			);
// 			while (LogList.Count > 100)
// 				LogList.RemoveAt(0);
// 			onLog.Invoke();
// 			#endif
// 		}
//
// 		public void Prepare() {
// 			Engine = new Engine(
// 				ctx => {
// 					ctx.LimitMemory(4_194_304);
// 					ctx.LimitRecursion(1024);
// 					ctx.Constraint(_constraint);
// 				}
// 			);
//
// 			Engine.SetValue("GameObject", TypeReference.CreateTypeReference(Engine, typeof(GameObject)));
// 			Engine.SetValue("Vector3", TypeReference.CreateTypeReference(Engine, typeof(Vector3)));
// 			Engine.SetValue("Vector2", TypeReference.CreateTypeReference(Engine, typeof(Vector2)));
// 			Engine.SetValue("Quaternion", TypeReference.CreateTypeReference(Engine, typeof(Quaternion)));
// 			Engine.SetValue("Transform", TypeReference.CreateTypeReference(Engine, typeof(Transform)));
//
// 			Engine.SetValue("gameObject", new ObjectWrapper(Engine, gameObject));
// 			Engine.SetValue("transform", new ObjectWrapper(Engine, transform));
//
// 			Engine.AddModule(
// 				"api", builder => builder
// 					.ExportFunction("setData", SetData)
// 					.ExportFunction("getData", GetData)
// 					.ExportFunction("getDataKeys", GetDataKeys)
// 			);
//
// 			Engine.AddModule(
// 				"logger", builder => builder
// 					.ExportFunction("log", objets => Log(LogType.Log, objets))
// 					.ExportFunction("warn", objets => Log(LogType.Warning, objets))
// 					.ExportFunction("error", objets => Log(LogType.Error, objets))
// 			);
//
// 			try {
// 				var module = Engine.PrepareModule(GetScriptCode());
// 				Engine.AddModule("__main__", x => x.AddModule(module));
// 				ExecutionContext = Engine.ImportModule("__main__");
// 				InvokeConst("onPrepare");
// 			} catch (Exception e) {
// 				Logger.LogError($"Error executing onPrepare function: {e.Message}");
// 				Engine           = null;
// 				ExecutionContext = null;
// 			}
// 		}
//
// 		public void Start()
// 			=> InvokeConst("onStart");
//
// 		public void Update()
// 			=> InvokeConst("onUpdate");
//
// 		public void FixedUpdate()
// 			=> InvokeConst("onFixedUpdate");
//
// 		public void LateUpdate()
// 			=> InvokeConst("onLateUpdate");
//
// 		public void OnDestroy()
// 			=> InvokeConst("onDestroy");
//
// 		public void OnEnable()
// 			=> InvokeConst("onEnable");
//
// 		private void InvokeConst(string methodName, params object[] args) {
// 			if (Engine == null) return;
// 			try {
// 				var method = ExecutionContext.Get(methodName);
// 				if (method.IsUndefined()) return;
// 				Engine.Invoke(method, args);
// 			} catch (Exception e) {
// 				Logger.LogError($"Error executing {methodName} function: {e.Message}");
// 			}
// 		}
//
// 		public void OnDisable()
// 			=> InvokeConst("onDisable");
//
// 		void Dispose() {
// 			if (Engine == null) return;
// 			InvokeConst("onDispose");
// 			Engine.Dispose();
// 			ExecutionContext = null;
// 			Engine           = null;
// 		}
//
// 		#if UNITY_EDITOR
// 		public bool     isCompiled;
// 		public JintFile scriptAsset;
//
// 		public void Compile() {
// 			data_SerializedScript = Convert.ToBase64String(
// 				System.Text.Encoding.UTF8.GetBytes(
// 					GetScriptCode()
// 				)
// 			);
// 			EditorUtility.SetDirty(this);
// 			isCompiled = true;
// 		}
//
// 		public string GetScriptCode()
// 			=> isCompiled
// 				? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(data_SerializedScript))
// 				: ReadScriptFile();
//
// 		public string ReadScriptFile()
// 			=> scriptAsset?.text ?? string.Empty;
//
// 		public void OnValidate() {
// 			if (isCompiled)
// 				return;
// 			Dispose();
// 			Prepare();
// 		}
//
// 		[InitializeOnLoadMethod]
// 		public static void OnStartInEditor() {
// 			EditorApplication.update += OnUpdateInEditor;
// 		}
//
// 		private DateTimeOffset lastFileUpdate = DateTimeOffset.MinValue;
//
// 		public JintScript(JintConstraint constraint) {
// 			_constraint = constraint;
// 		}
//
// 		private static DateTimeOffset _lastUpdate = DateTimeOffset.MinValue;
//
// 		private static void OnUpdateInEditor() {
// 			// if (DateTimeOffset.Now - _lastUpdate < TimeSpan.FromSeconds(1)) return;
// 			// _lastUpdate = DateTimeOffset.Now;
// 			// var scripts = new List<JintScript>();
// 			// for (var sceneIndex = 0; sceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCount; sceneIndex++) {
// 			// 	var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(sceneIndex);
// 			// 	foreach (var rootGameObject in scene.GetRootGameObjects())
// 			// 		scripts.AddRange(rootGameObject.GetComponentsInChildren<JintScript>());
// 			// }
// 			//
// 			//
// 			// foreach (var script in scripts) {
// 			// 	if (script.IsCompiled) continue;
// 			// 	var lastWriteTime = File.GetLastWriteTime(script.scriptAsset ? AssetDatabase.GetAssetPath(script.scriptAsset) : string.Empty);
// 			// 	if (lastWriteTime <= script.lastFileUpdate) continue;
// 			// 	script.lastFileUpdate = lastWriteTime;
// 			// 	script.OnValidate();
// 			// }
// 		}
// 		#else
//         public string GetScriptCode()
//             => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(data_SerializedScript));
// 		#endif // UNITY_EDITOR
// 	}
// }