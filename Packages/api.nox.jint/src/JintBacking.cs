using System;
using System.Collections.Generic;
using System.Linq;
using Jint;
using Jint.Native.Object;
using Jint.Runtime.Interop;
using Nox.CCK.Build;
using Nox.CCK.Jint;
using UnityEngine;
using Engine = Jint.Engine;
using Transform = UnityEngine.Transform;
using NoxLogger = Nox.CCK.Utils.Logger;

namespace api.nox.jint {
	[RequireComponent(typeof(JintScript))]
	public class JintBacking : MonoBehaviour, IJintBacking, ICompilable {
		public Engine         Engine;
		public JintScript     script;
		public ObjectInstance ExecutionContext;
		public Logger         Logger;

		public void Compile()
			=> DestroyImmediate(this);

		public string[] GetProperties()
			=> ExecutionContext != null
				? ExecutionContext.GetOwnPropertyKeys()
					.Select(kvp => kvp.ToString())
					.ToArray()
				: Array.Empty<string>();

		public object GetProperty(string propertyName) {
			if (ExecutionContext == null) return null;
			try {
				var prop = ExecutionContext.Get(propertyName);
				return prop.IsUndefined() ? null : prop.ToObject();
			} catch (Exception e) {
				NoxLogger.LogError($"Error getting property {propertyName}: {e.Message}", this);
				return null;
			}
		}

		private void OnValidate() {
			script ??= GetComponent<JintScript>();
			if (Engine == null) Prepare();
		}

		// ReSharper disable Unity.PerformanceAnalysis
		public void Prepare() {
			script ??= GetComponent<JintScript>();
			Logger ??= new Logger();

			NoxLogger.Log("Prepare");
			Engine = new Engine(
				ctx => {
					ctx.LimitMemory(4_194_304);
					ctx.LimitRecursion(1024);
				}
			);

			Engine.SetValue("GameObject", TypeReference.CreateTypeReference(Engine, typeof(GameObject)));
			Engine.SetValue("Vector3", TypeReference.CreateTypeReference(Engine, typeof(Vector3)));
			Engine.SetValue("Vector2", TypeReference.CreateTypeReference(Engine, typeof(Vector2)));
			Engine.SetValue("Quaternion", TypeReference.CreateTypeReference(Engine, typeof(Quaternion)));
			Engine.SetValue("Transform", TypeReference.CreateTypeReference(Engine, typeof(Transform)));

			// import json of Script.exports
			if (!string.IsNullOrEmpty(script.exports)) {
				try {
					var exports = Engine.Evaluate(script.exports).AsObject();
					Engine.SetValue("exports", exports);
				} catch (Exception e) {
					NoxLogger.LogError($"Error parsing exports: {e.Message}", this);
				}
			} else Engine.SetValue("exports", new ObjectWrapper(Engine, new Dictionary<string, object>()));

			Engine.AddModule(
				"console", builder => builder
					.ExportFunction("log", objets => Logger.Log(LogType.Log, string.Join(" ", objets.Select(e => e.ToString()))))
					.ExportFunction("warn", objets => Logger.Log(LogType.Warning, string.Join(" ", objets.Select(e => e.ToString()))))
					.ExportFunction("error", objets => Logger.Log(LogType.Error, string.Join(" ", objets.Select(e => e.ToString()))))
			);

			Engine.AddModule(
				"behaviour", builder => builder
					.ExportObject("transform", new ObjectWrapper(Engine, transform))
					.ExportObject("gameObject", new ObjectWrapper(Engine, gameObject))
			);
			try {
				NoxLogger.LogDebug($"script: {script}");
				NoxLogger.LogDebug($"script.asset: {script.asset}");
				NoxLogger.LogDebug($"script.asset.text: {script.asset.text}");

				var module = Engine.PrepareModule(script.asset.text);
				Engine.AddModule("__main__", x => x.AddModule(module));
				ExecutionContext = Engine.ImportModule("__main__");
				Invoke("onPrepare");
			} catch (Exception e) {
				NoxLogger.LogError($"Error executing onPrepare function: {e.Message}", this);
				NoxLogger.LogException(e, this);
				Engine           = null;
				ExecutionContext = null;
			}
		}

		private void OnDestroy() {
			if (Engine == null) return;
			try {
				Invoke("onDestroy");
			} catch (Exception e) {
				NoxLogger.LogError($"Error executing onDestroy function: {e.Message}", this);
			}

			Engine.Dispose();
			Engine           = null;
			ExecutionContext = null;
		}

		public void Invoke(string methodName, params object[] args) {
			if (Engine == null) Prepare();
			if (Engine == null) return;
			try {
				var method = ExecutionContext.Get(methodName);
				if (method.IsUndefined()) return;
				Engine.Invoke(method, args);
			} catch (Exception e) {
				NoxLogger.LogError($"Error executing {methodName} function: {e.Message}", this);
			}
		}

		public object Call(string functionName, object[] args) {
			if (Engine == null) Prepare();
			if (Engine == null) return null;
			try {
				var method = ExecutionContext.Get(functionName);
				return !method.IsUndefined()
					? Engine.Invoke(method, args)
					: null;
			} catch (Exception e) {
				NoxLogger.LogError($"Error executing {functionName} function: {e.Message}", this);
				return null;
			}
		}

		public T Call<T>(string functionName, object[] args) {
			if (Engine == null) Prepare();
			if (Engine == null) return default;
			try {
				var method = ExecutionContext.Get(functionName);
				if (method.IsUndefined()) return default;
				var result = Engine.Invoke(method, args);
				return (T)result.ToObject();
			} catch (Exception e) {
				NoxLogger.LogError($"Error executing {functionName} function: {e.Message}", this);
				return default;
			}
		}
	}
}