using Jint.Native.Object;
using UnityEngine;
using Jint;
using Jint.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Function;
using Jint.Runtime;
using Jint.Runtime.Modules;
using Nox.CCK.Network;
using Nox.CCK.Utils;
using Nox.Jint;
using Nox.Players;
using Nox.Sessions;
using JintEngine = Jint.Engine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace api.nox.session.jint {
	public class JintBackingSession : MonoBehaviour, IJintBacking {
		public JintBackingModule module;
		public IJintScript Script;
		public ObjectInstance Context;

		private JintEngine _engine;

		/// <summary>
		/// Converts a JsValue (especially ArrayInstance) to byte array
		/// </summary>
		private static byte[] JsValueToByteArray(JsValue value) {
			if (!value.IsObject() || value.AsObject() is not ArrayInstance arrayInstance)
				return (byte[])value.ToObject();

			var length = (int)arrayInstance.Length;
			var buffer = new byte[ length ];

			for (var i = 0; i < length; i++) {
				var element = arrayInstance[(uint)i];
				buffer[i] = element.IsNumber() ? (byte)(element.AsNumber() % 256) : (byte)0;
			}

			return buffer;
		}

		private static JsValue ConvertObject(JintEngine engine, object arg)
			=> arg switch {
				null                        => JsValue.Null,
				_ when arg.GetType().IsEnum => JsValue.FromObject(engine, ConvertObjects(engine, arg)),
				_                           => JsValue.FromObject(engine, arg),
			};

		private static JsValue[] ConvertObjects(JintEngine engine, params object[] args) {
			var jsArgs = new JsValue[ args.Length ];
			for (var i = 0; i < args.Length; i++)
				jsArgs[i] = ConvertObject(engine, args[i]);
			return jsArgs;
		}

		public void Initialize() {
			if (_engine != null)
				return;

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

				// add Buffer of nodejs
				_engine.AddModule("buffer", builder => builder
					.ExportFunction("from", args => {
						var data     = args.At(0).AsString();
						var encoding = args.Length > 1 ? args.At(1).AsString() : "utf8";
						return JsValue.FromObject(_engine, NodeBufferImpl.from(data, encoding));
					})
					.ExportFunction("toString", args => {
						var buffer   = JsValueToByteArray(args.At(0));
						var encoding = args.Length > 1 ? args.At(1).AsString() : "utf8";
						return JsValue.FromObject(_engine, NodeBufferImpl.toString(buffer, encoding));
					})
				);
				_engine.SetValue("Buffer", TypeReference.CreateTypeReference(_engine, typeof(NodeBufferImpl)));

				_engine.AddModule(
					"console", builder => builder
						.ExportFunction(
							"log", objets => Logger.Log(
								string.Join(" ", objets.Select(e => e.ToString())),
								this,
								$"{GetType().Name}_{GetEntityId().GetHashCode()}"
							)
						)
						.ExportFunction(
							"warn", objets => Logger.LogWarning(
								string.Join(" ", objets.Select(e => e.ToString())),
								this,
								$"{GetType().Name}_{GetEntityId().GetHashCode()}"
							)
						)
						.ExportFunction(
							"error", objets => Logger.LogError(
								string.Join(" ", objets.Select(e => e.ToString())),
								this,
								$"{GetType().Name}_{GetEntityId().GetHashCode()}"
							)
						)
				);

				_engine.AddModule(
					"behaviour", builder => builder
						.ExportObject("gameObject", gameObject)
						.ExportObject("transform", gameObject.transform)
						.ExportObject("rigidbody", gameObject.GetComponent<Rigidbody>())
						.ExportObject("id", GetEntityId().GetHashCode())
				);

				_engine.AddModule(
					"tables", builder => builder
						.ExportFunction("getPrivate", () => ToPromise(GetTable(false).AsTask()))
						.ExportFunction("getPublic", () => ToPromise(GetTable(true).AsTask()))
						.ExportFunction("setPrivate", args => ToPromise(SetTable(false, args).AsTask()))
						.ExportFunction("setPublic", args => ToPromise(SetTable(true, args).AsTask()))
						.ExportFunction("delPrivate", () => ToPromise(DeleteTable(false).AsTask()))
						.ExportFunction("delPublic", () => ToPromise(DeleteTable(true).AsTask()))
				);

				_engine.AddModule(
					"players", builder => builder
						.ExportFunction(
							"getLocal", () => {
								var player = module.Session.LocalPlayer;
								return player != null
									? new ObjectWrapper(_engine, player)
									: JsValue.Null;
							}
						)
						.ExportFunction(
							"getMaster", () => {
								var player = module.Session.MasterPlayer;
								return player != null
									? new ObjectWrapper(_engine, player)
									: JsValue.Null;
							}
						)
						.ExportFunction("getAll", () => module.Session.Entities.GetEntities<IPlayer>())
						.ExportFunction("getCount", () => module.Session.Entities.GetCount<IPlayer>())
						.ExportFunction(
							"getAt", args => {
								var players = module.Session.Entities.GetEntities<IPlayer>();
								var index   = players.ElementAtOrDefault((int)args.At(0).AsNumber());
								return index != null
									? new ObjectWrapper(_engine, index)
									: JsValue.Null;
							}
						)
				);

				var netSession = module.Session as INetSession;
				_engine.AddModule(
					"network", builder => builder
						.ExportFunction("getTime", () => JsValue.FromObject(_engine, netSession?.Time ?? DateTime.UtcNow))
						.ExportFunction("isConnected", () => netSession?.IsConnected ?? false)
						.ExportFunction("eventToHash", args => JsValue.FromObject(_engine, Hash.CRC64(args.At(0).AsString())))
						.ExportFunction(
							"emitEvent", args => {
								if (netSession == null) {
									Logger.LogWarning("Network adapter is null", this);
									return false;
								}

								var @event = args.At(0).IsNumber()
									? (long)args.At(0).AsNumber()
									: Hash.CRC64(args.At(0).AsString());
								byte[] raw;

								var dataArg = args.At(1);
								if (dataArg.IsUndefined() || dataArg.IsNull())
									raw = Array.Empty<byte>();
								else if (!dataArg.IsObject()) {
									Logger.LogWarning($"data argument is not an object (type: {dataArg.Type})", this);
									return false;
								} else {
									var obj = dataArg.AsObject();
									if (obj is not ArrayInstance arrayInstance) {
										Logger.LogWarning("data argument is not an array", this);
										return false;
									}

									var length = (int)arrayInstance.Length;
									raw = new byte[ length ];
									for (var i = 0; i < length; i++) {
										var element = arrayInstance[(uint)i];
										if (element.IsNumber()) {
											var num = element.AsNumber();
											raw[i] = (byte)(num % 256); // Ensure it's within byte range
										} else if (element.IsString()) {
											// Try to parse string as number
											if (double.TryParse(element.AsString(), out var parsed))
												raw[i] = (byte)(parsed % 256);
											else
												raw[i] = 0;
										} else
											raw[i] = 0;

									}
								}

								var emitting = netSession.EmitEvent(@event, raw).AsTask();
								if (emitting.IsCompletedSuccessfully)
									return JsValue.FromObject(_engine, emitting.Result);
								if (emitting.IsFaulted) {
									Logger.LogError($"Error emitting event '{@event}': {emitting.Exception}", this);
									return false;
								}

								if (emitting.IsCanceled) {
									Logger.LogWarning($"Emitting event '{@event}' was canceled", this);
									return false;
								}

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
											Logger.LogError($"Error emitting event '{@event}': {t.Exception}", this);
											_engine.Invoke(reject!, false);
										} else
											_engine.Invoke(resolve!, JsValue.FromObject(_engine, t.Result));
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
				Logger.LogError(e, this);
			}
		}

		private bool TryTableKey(bool isPublic, out string key) {
			var id = module.Session.Dimensions.Identifier;
			if (!id.IsValid()) {
				key = null;
				return false;
			}
			key = $"{(isPublic ? "public." : "")}worlds.{Uri.EscapeDataString(id.ToShortString(true))}";
			return true;
		}

		private async UniTask<JsValue> GetTable(bool isPublic) {
			var id = module.Session.Dimensions.Identifier;
			if (!TryTableKey(isPublic, out var key))
				return JsValue.Null;
			var entry = await Main.TableAPI.Get(key);
			return new ObjectWrapper(_engine, entry == null ? JsValue.Null : entry.AsBytes);
		}

		private async UniTask<JsValue> SetTable(bool isPublic, JsValue[] args) {
			var id = module.Session.Dimensions.Identifier;
			if (args.Length == 0 || !TryTableKey(isPublic, out var key))
				return JsValue.Null;
			var entry = await Main.TableAPI.Set(key, JsValueToByteArray(args[0]), "application/octet-stream+world");
			return new ObjectWrapper(_engine, entry == null ? JsValue.Null : entry.AsBytes);
		}

		private async UniTask<JsValue> DeleteTable(bool isPublic) {
			var id = module.Session.Dimensions.Identifier;
			if (!TryTableKey(isPublic, out var key))
				return JsBoolean.False;
			return await Main.TableAPI.Delete(key)
				? JsBoolean.True
				: JsBoolean.False;
		}

		private JsValue CreatePromise(out FunctionInstance resolve, out FunctionInstance reject) {
			var promiseFactory = _engine.Evaluate(
					@"(function() {
							var resolve, reject;
							var p = new Promise(function(res, rej) { resolve = res; reject = rej; });
							return { promise: p, resolve: resolve, reject: reject };
					})"
				)
				.AsObject();
			resolve = promiseFactory.Get("resolve") as FunctionInstance;
			reject  = promiseFactory.Get("reject") as FunctionInstance;
			return promiseFactory.Get("promise");
		}

		private JsValue ToPromise<T>(Task<T> task) {
			var promise = CreatePromise(out var resolve, out var reject);
			task.ContinueWith(t => {
				if (t.IsFaulted || t.IsCanceled) {
					Logger.LogError($"Error in task: {t.Exception}", this);
					_engine.Invoke(reject!, false);
					return;
				}
				_engine.Invoke(resolve!, JsValue.FromObject(_engine, t.Result));
			});
			return promise;
		}

		private void SetExports(string property, object value) {
			try {
				if (_engine == null || Context == null)
					return;
				var export = Context.Get("exports");
				if (export.IsUndefined())
					export = new ObjectWrapper(_engine, new Dictionary<string, object>());
				if (!export.IsObject())
					return;
				var obj = export.AsObject();
				obj.Set(property, new ObjectWrapper(_engine, value), true);
			} catch (Exception e) {
				Logger.LogError(new Exception($"Error setting export '{property}': {e.Message}", e), this);
			}
		}

		public void Invoke(string method, params object[] args) {
			try {
				if (_engine == null || Context == null) {
					Logger.LogWarning("Engine or Context is null", this);
					return;
				}

				var methodRef = Context.Get(method);
				if (methodRef.IsUndefined())
					return;

				_engine.Invoke(methodRef, args);
			} catch (Exception e) {
				Logger.LogError(new Exception($"Error invoking method '{method}'", e), this);
			}
		}

		public object Call(string method, object[] args) {
			try {
				if (_engine == null || Context == null) {
					Logger.LogWarning("Engine or Context is null", this);
					return null;
				}

				var methodRef = Context.Get(method);
				if (methodRef.IsUndefined())
					return null;

				// Convert arguments to JsValue to avoid InvalidCastException
				var jsArgs = new JsValue[ args.Length ];
				for (var i = 0; i < args.Length; i++) {
					if (args[i] is byte[] bytes) {
						var jsArray = _engine.Realm.Intrinsics.Array.Construct(bytes.Length);
						for (var j = 0; j < bytes.Length; j++) {
							jsArray[(uint)j] = JsValue.FromObject(_engine, bytes[j]);
						}
						jsArgs[i] = jsArray;
					} else if (args[i] is IPlayer player) {
						jsArgs[i] = new ObjectWrapper(_engine, player);
					} else {
						jsArgs[i] = JsValue.FromObject(_engine, args[i]);
					}
				}

				return _engine.Invoke(methodRef, jsArgs);
			} catch (Exception e) {
				Logger.LogError(new Exception($"Error invoking method '{method}'", e), this);
				return null;
			}
		}

		public T Call<T>(string method, object[] args) {
			try {
				if (_engine == null || Context == null) {
					Logger.LogWarning("Engine or Context is null", this);
					return default;
				}

				var methodRef = Context.Get(method);
				if (methodRef.IsUndefined())
					return default;

				// Convert arguments to JsValue to avoid InvalidCastException
				var jsArgs = new JsValue[ args.Length ];
				for (var i = 0; i < args.Length; i++) {
					if (args[i] is byte[] bytes) {
						var jsArray = _engine.Realm.Intrinsics.Array.Construct(bytes.Length);
						for (var j = 0; j < bytes.Length; j++) {
							jsArray[(uint)j] = JsValue.FromObject(_engine, bytes[j]);
						}
						jsArgs[i] = jsArray;
					} else if (args[i] is IPlayer player) {
						jsArgs[i] = new ObjectWrapper(_engine, player);
					} else {
						jsArgs[i] = JsValue.FromObject(_engine, args[i]);
					}
				}

				var result = _engine.Invoke(methodRef, jsArgs);
				return (T)result.ToObject();
			} catch (Exception e) {
				Logger.LogError(new Exception($"Error invoking method '{method}'", e), this);
				return default;
			}
		}

		private void OnDestroy() {
			if (_engine == null)
				return;
			Main.CoreAPI.EventAPI.Emit("jint_engine_destroyed", this, _engine);
			_engine.Dispose();
			_engine = null;
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

		public void OnEvent(long @event, byte[] raw, IPlayer sender)
			=> Invoke("onEvent", @event, raw, sender);
	}

	public interface NodeBuffer {
		byte[] from(string data);
		byte[] from(string data, string encoding);
		string toString();
		string toString(string encoding);
	}

	public interface NodeHash {
		int crc32(byte[]  data);
		int crc32(string  data);
		long crc64(byte[] data);
		long crc64(string data);
	}

	public static class NodeBufferImpl {
		public static byte[] from(string data)
			=> from(data, "utf8");

		public static byte[] from(string data, string encoding) {
			return encoding.ToLower() switch {
				"utf8"    => System.Text.Encoding.UTF8.GetBytes(data),
				"ascii"   => System.Text.Encoding.ASCII.GetBytes(data),
				"unicode" => System.Text.Encoding.Unicode.GetBytes(data),
				"base64"  => Convert.FromBase64String(data),
				"hex" => Enumerable.Range(0, data.Length / 2)
					.Select(x => Convert.ToByte(data.Substring(x * 2, 2), 16))
					.ToArray(),
				_ => throw new NotSupportedException($"Encoding '{encoding}' is not supported"),
			};
		}

		public static string toString(byte[] buffer)
			=> toString(buffer, "utf8");

		public static string toString(byte[] buffer, string encoding) {
			return encoding.ToLower() switch {
				"utf8"    => System.Text.Encoding.UTF8.GetString(buffer),
				"ascii"   => System.Text.Encoding.ASCII.GetString(buffer),
				"unicode" => System.Text.Encoding.Unicode.GetString(buffer),
				"base64"  => Convert.ToBase64String(buffer),
				"hex"     => BitConverter.ToString(buffer).Replace("-", "").ToLower(),
				_         => throw new NotSupportedException($"Encoding '{encoding}' is not supported"),
			};
		}
	}

	public static class NodeHashImpl {
		public static int crc32(byte[] data)
			=> Hash.CRC32(data);

		public static int crc32(string data)
			=> Hash.CRC32(data);

		public static long crc64(byte[] data)
			=> Hash.CRC64(data);

		public static long crc64(string data)
			=> Hash.CRC64(data);
	}
}