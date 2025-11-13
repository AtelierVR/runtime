using Jint.Native.Object;
using UnityEngine;
using Jint;
using Jint.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Jint.Native;
using Jint.Native.Function;
using Jint.Runtime;
using Jint.Runtime.Modules;
using Nox.Jint;
using Nox.Players;
using Nox.Sessions;
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
						ctx.EnableModules(new DefaultModuleLoader(Main.JintAPI.GetModulesPath()));
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
						.ExportObject("rigidbody", gameObject.GetComponent<Rigidbody>())
						.ExportFunction("id", () => GetInstanceID())
				);
				
				_engine.AddModule(
					"players", builder => builder
						.ExportFunction("getLocal", () => module.Session.GetAdapter().GetLocalPlayer())
						.ExportFunction("getMaster", () => module.Session.GetAdapter().GetMasterPlayer())
						.ExportFunction("getAll", () => module.Session.GetAdapter().GetPlayers())
						.ExportFunction("getCount", () => module.Session.GetAdapter().GetPlayerCount())
						.ExportFunction("getAt", args => new ObjectWrapper(_engine, module.Session.GetAdapter().GetPlayer((int)args.At(0).AsNumber())))
				);

				var netAdapter = module.Session.GetAdapter() as INetworkedAdapter;
				_engine.AddModule(
					"network", builder => builder
						.ExportFunction("getTime", () => JsValue.FromObject(_engine, netAdapter?.GetTime() ?? DateTime.Now))
						.ExportFunction("isConnected", () => netAdapter?.IsConnected() ?? false)
						.ExportFunction("getLatency", () => netAdapter?.GetLatency()   ?? 0.0)
						.ExportFunction(
							"emitEvent", args => {
								if (netAdapter == null)
									return false;

								var eventName = args.At(0).AsString();
								var eventData = !args.At(1).IsUndefined()
									? args.At(1).ToObject() as byte[]
									: Array.Empty<byte>();

								var emitting = netAdapter.EmitEvent(eventName, eventData).AsTask();
								if (emitting.IsCompletedSuccessfully)
									return JsValue.FromObject(_engine, emitting.Result);
								if (emitting.IsFaulted)
									return false;
								if (emitting.IsCanceled)
									return false;

								var promiseFactory = _engine.Evaluate(
										@"(function() {
												var resolve, reject;
												var p = new Promise(function(res, rej) { resolve = res; reject = rej; });
												return { promise: p, resolve: resolve, reject: reject };
										})"
									)
									.AsObject();

								var promise = promiseFactory.Get("promise");
								var resolve = promiseFactory.Get("resolve") as FunctionInstance;
								var reject  = promiseFactory.Get("reject") as FunctionInstance;

								emitting.ContinueWith(
									t => {
										if (t.IsFaulted || t.IsCanceled) {
											_engine.Invoke(reject!, false);
										} else _engine.Invoke(resolve!, JsValue.FromObject(_engine, t.Result));
									}
								);

								return promise;
							}
						)
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
				if (_engine == null || Context == null) return;
				var export  = Context.Get("exports");
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
				if (_engine == null || Context == null) return;
				var methodRef = Context.Get(method);
				if (methodRef.IsUndefined()) return;
				_engine.Invoke(methodRef, args);
			} catch (Exception e) {
				Logger.LogError($"Error invoking method '{method}': {e.Message}", this);
				Logger.LogException(e, this);
			}
		}

		public object Call(string method, object[] args) {
			try {
				if (_engine == null || Context == null) return null;
				var methodRef = Context.Get(method);
				return methodRef.IsUndefined()
					? null
					: _engine.Invoke(methodRef, args);
			} catch (Exception e) {
				Logger.LogError($"Error invoking method '{method}': {e.Message}", this);
				Logger.LogException(e, this);
				return null;
			}
		}

		public T Call<T>(string method, object[] args) {
			try {
				if (_engine == null || Context == null) return default;
				var methodRef = Context.Get(method);
				if (methodRef.IsUndefined()) return default;
				var result = _engine.Invoke(methodRef, args);
				return (T)result.ToObject();
			} catch (Exception e) {
				Logger.LogError($"Error invoking method '{method}': {e.Message}", this);
				Logger.LogException(e, this);
				return default;
			}
		}

		private void OnDestroy() {
			if (_engine == null) return;
			Main.CoreAPI.EventAPI.Emit("jint_engine_destroyed", this, _engine);
			_engine.Dispose();
			_engine  = null;
			Context = null;
		}

		public void OnSessionSelected()
			=> Invoke("onSessionSelected");

		public void OnSessionDeselected()
			=> Invoke("onSessionDeselected");

		public void OnPlayerJoined(IPlayer player)
			=> Invoke("onPlayerJoined", player);

		public void OnPlayerLeft(IPlayer player)
			=> Invoke("onPlayerLeft", player);

		public void OnAuthorityTransferred(IPlayer player)
			=> Invoke("onAuthorityTransferred", player);

		public void OnEventTriggered(string @event, byte[] raw, IPlayer sender)
			=> Invoke("onEvent", @event, raw, sender);
	}
}