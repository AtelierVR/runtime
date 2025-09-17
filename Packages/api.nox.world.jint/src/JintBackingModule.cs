using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Jint;
using Jint.Runtime.Interop;
using Nox.CCK.Jint;
using Nox.CCK.Utils;
using Nox.Sessions;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using JintEngine = Jint.Engine;
using Transform = UnityEngine.Transform;

namespace api.nox.session.jint {
	public class JintBackingModule : MonoBehaviour, ISessionModule {
		#region Internal

		public static bool Check(IWorldDescriptor descriptor) {
			var modules = descriptor.GetModules<JintBackingModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.GetAnchor().AddComponent<JintBackingModule>(),
				_ => null
			};

			if (!module) {
				Logger.LogError("Verify that the World prefab has a valid FellInVoidWorldModule component.");
				return false;
			}

			return true;
		}

		public UniTask<bool> Setup(IRuntimeWorld runtime)
			=> UniTask.FromResult(true);

		#endregion

		#region Jint Backing

		public void Invoke(JintBackingSession initiator, string methodName, params object[] args) {
			Prepare();

			try {
				var method = initiator.Context.Get(methodName);
				if (method.IsUndefined()) return;
				_engine.Invoke(method, args);
			} catch (Exception e) {
				Logger.LogError(e, this);
			}
		}

		public object Call(JintBackingSession initiator, string functionName, object[] args) {
			Prepare();

			try {
				var method = initiator.Context.Get(functionName);
				return !method.IsUndefined()
					? _engine.Invoke(method, args)
					: null;
			} catch (Exception e) {
				Logger.LogError(e, this);
				return null;
			}
		}

		public T Call<T>(JintBackingSession initiator, string functionName, object[] args) {
			Prepare();

			try {
				var method = initiator.Context.Get(functionName);
				if (method.IsUndefined()) return default;
				var result = _engine.Invoke(method, args);
				return (T)result.ToObject();
			} catch (Exception e) {
				Logger.LogError(e, this);
				return default;
			}
		}

		#endregion

		private ISession                 _session;
		public  List<JintBackingSession> backings = new();
		private JintEngine               _engine;

		public void OnSceneLoaded(IWorldDescriptor _0, int _1, GameObject anchor) {
			Prepare();
			var scripts = anchor.GetComponentsInChildren<JintScript>(true);
			foreach (var script in scripts) {
				if (backings.Any(b => b.GetInstanceID() == script.GetInstanceID())) continue;
				var backing = script.gameObject.GetOrAddComponent<JintBackingSession>();
				backing.module = this;
				backing.script = script;
				InitializeBacking(backing);
				backings.Add(backing);
			}
		}

		private void InitializeBacking(JintBackingSession backing) {
			Prepare();

			try {
				var module = JintEngine.PrepareModule(backing.script.asset.text);
				_engine.AddModule($"__jint_{backing.GetInstanceID()}__", x => x.AddModule(module));
				backing.Context = _engine.ImportModule($"__jint_{backing.GetInstanceID()}__");

				if (!string.IsNullOrEmpty(backing.script.exports))
					try {
						var exports = _engine.Evaluate(backing.script.exports).AsObject();
						foreach (var prop in exports.GetOwnProperties())
							if (prop.Value?.Value != null)
								SetExports(backing, prop.Key.AsString(), prop.Value.Value.ToObject());
					} catch (Exception e) {
						Logger.LogError(e, this);
					}

				Invoke(backing, "onInitialize");
			} catch (Exception e) {
				Logger.LogException(e, this);
			}
		}

		private void SetExports(JintBackingSession backing, string property, object value) {
			try {
				var context = backing.Context;
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

		private void Prepare() {
			if (_engine != null) return;
			_engine = new JintEngine(
				ctx => {
					ctx.LimitMemory(4_194_304);
					ctx.LimitRecursion(1024);
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
					.ExportObject("transform", new ObjectWrapper(_engine, transform))
					.ExportObject("gameObject", new ObjectWrapper(_engine, gameObject))
			);
			
			Main.CoreAPI.EventAPI.Emit("jint_engine_created", this, _engine);
		}

		public void OnSceneUnloaded(int index)
			=> backings.RemoveAll(b => !b);

		public void OnLoaded(ISession session)
			=> _session = session;

		private void OnDestroy() {
			Main.CoreAPI.EventAPI.Emit("jint_engine_destroyed", this, _engine);
			foreach (var backing in backings.Where(backing => backing))
				Destroy(backing);
			backings.Clear();
			_engine  = null;
			_session = null;
		}
	}
}