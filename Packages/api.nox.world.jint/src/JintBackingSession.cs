using Jint.Native.Object;
using UnityEngine;
using Jint;
using Jint.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using Jint.Runtime.Modules;
using Nox.Jint;
using Nox.Sessions;
using UnityEngine.Events;
using JintEngine = Jint.Engine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace api.nox.session.jint {
	public class JintBackingSession : MonoBehaviour, IJintBacking {
		public JintBackingModule module;
		public IJintScript       Script;
		public ObjectInstance    Context;

		private JintEngine _engine;

		public void Initialize() {
			if (_engine != null) return;

			try {
				_engine = new JintEngine(
					ctx => {
						ctx.LimitMemory(4_194_304);
						ctx.LimitRecursion(1024);
						ctx.EnableModules(new DefaultModuleLoader(Main.GetModulePath()));
					}
				);

				_engine.SetValue("GameObject", TypeReference.CreateTypeReference(_engine, typeof(GameObject)));
				_engine.SetValue("Vector3", TypeReference.CreateTypeReference(_engine, typeof(Vector3)));
				_engine.SetValue("Vector2", TypeReference.CreateTypeReference(_engine, typeof(Vector2)));
				_engine.SetValue("Quaternion", TypeReference.CreateTypeReference(_engine, typeof(Quaternion)));
				_engine.SetValue("Transform", TypeReference.CreateTypeReference(_engine, typeof(Transform)));

				_engine.AddModule(
					"console", builder => builder
						.ExportFunction(
							"log", objets => Logger.Log(
								string.Join(" ", objets.Select(e => e.ToString())),
								$"{GetType().Name}_{GetInstanceID()}"
							)
						)
						.ExportFunction(
							"warn", objets => Logger.LogWarning(
								string.Join(" ", objets.Select(e => e.ToString())),
								$"{GetType().Name}_{GetInstanceID()}"
							)
						)
						.ExportFunction(
							"error", objets => Logger.LogError(
								string.Join(" ", objets.Select(e => e.ToString())),
								$"{GetType().Name}_{GetInstanceID()}"
							)
						)
				);

				_engine.AddModule(
					"behaviour", builder => builder
						.ExportObject("gameObject", gameObject)
						.ExportObject("transform", gameObject.transform)
						.ExportFunction("id", () => GetInstanceID())
				);

				var m = JintEngine.PrepareModule(Script.GetContent());
				_engine.AddModule("__main__", x => x.AddModule(m));
				Context = _engine.ImportModule("__main__");

				try {
					var exports = Script.GetExports();
					foreach (var prop in exports)
						SetExports(prop.Key, prop.Value);
				} catch (Exception e) {
					Logger.LogError(e, this);
				}

				Main.CoreAPI.EventAPI.Emit("jint_engine_created", this, _engine);
			} catch (Exception e) {
				Logger.LogException(e, this);
			}
		}

		private void SetExports(string property, object value) {
			try {
				var context = Context;
				var export  = context.Get("exports");
				if (export.IsUndefined())
					export = new ObjectWrapper(_engine, new Dictionary<string, object>());
				if (!export.IsObject())
					return;
				var obj = export.AsObject();
				obj.Set(property, new ObjectWrapper(_engine, value), true);
			} catch (Exception e) {
				Logger.LogException(e, this);
			}
		}

		public void Invoke(string method, params object[] args) {
			try {
				if (_engine == null) return;
				var methodRef = Context.Get(method);
				if (methodRef.IsUndefined()) return;
				_engine.Invoke(methodRef, args);
			} catch (Exception e) {
				Logger.LogError(e, this);
			}
		}

		public object Call(string method, object[] args) {
			try {
				if (_engine == null) return null;
				var methodRef = Context.Get(method);
				return methodRef.IsUndefined()
					? null
					: _engine.Invoke(methodRef, args);
			} catch (Exception e) {
				Logger.LogError(e, this);
				return null;
			}
		}

		public T Call<T>(string method, object[] args) {
			try {
				if (_engine == null) return default;
				var methodRef = Context.Get(method);
				if (methodRef.IsUndefined()) return default;
				var result = _engine.Invoke(methodRef, args);
				return (T)result.ToObject();
			} catch (Exception e) {
				Logger.LogError(e, this);
				return default;
			}
		}

		private void OnDestroy() {
			if (_engine == null) return;
			Main.CoreAPI.EventAPI.Emit("jint_engine_destroyed", this, _engine);
			_engine = null;
		}

		public void RemovePlayerLeftListener(UnityAction action) {
			throw new NotImplementedException();
		}

		public void RemovePlayerJoinedListener(UnityAction action) {
			throw new NotImplementedException();
		}
	}
}