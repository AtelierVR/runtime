using Jint.Native.Object;
using UnityEngine;
using Jint;
using Jint.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using Jint.Native;
using Jint.Native.Array;
using Jint.Runtime.Modules;
using Nox.CCK.Utils;
using Nox.Jint;
using Nox.Players;
using JintEngine = Jint.Engine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;
using api.nox.jint;

namespace api.nox.session.jint {
	public class JintBackingSession : MonoBehaviour, IJintBacking {
		public JintBackingModule module;
		public IJintScript       Script;
		public ObjectInstance    Context;

		/// <summary>
		/// Tags that identify the context this backing runs in (e.g. <c>"session"</c>, <c>"avatar"</c>).
		/// Only modules whose <c>Tags</c> list is empty or shares at least one tag with this list are bound.
		/// </summary>
		public string[] Tags = { "session" };

		private JintEngine _engine;
		private bool       _initialized;

		/// <summary>Expose the underlying engine for <see cref="JintScriptingContext"/>.</summary>
		internal JintEngine Engine => _engine;

		public void Initialize() {
			if (_initialized)
				return;

			try {
				_engine = new JintEngine(
					ctx => {
						ctx.LimitMemory(4_194_304);
						ctx.LimitRecursion(1024);
						ctx.EnableModules(new DefaultModuleLoader(Main.JintAPI.GetModulesPath()));
					}
				);

				// Jint-specific globals (TypeReference cannot be expressed as a generic module)
				_engine.SetValue("GameObject", TypeReference.CreateTypeReference(_engine, typeof(GameObject)));
				_engine.SetValue("Vector3",    TypeReference.CreateTypeReference(_engine, typeof(Vector3)));
				_engine.SetValue("Vector2",    TypeReference.CreateTypeReference(_engine, typeof(Vector2)));
				_engine.SetValue("Quaternion", TypeReference.CreateTypeReference(_engine, typeof(Quaternion)));
				_engine.SetValue("Transform",  TypeReference.CreateTypeReference(_engine, typeof(Transform)));
				_engine.SetValue("Buffer",     TypeReference.CreateTypeReference(_engine, typeof(NodeBufferImpl)));

				// Bind all modules registered in nox.scripting via the adapter
				var scriptingAPI = Main.ScriptingAPI;
				var context      = new JintScriptingContext(this, scriptingAPI);
				if (scriptingAPI != null)
					JintModuleAdapter.BindAllModules(_engine, context, scriptingAPI, Tags);
				else
					Logger.LogWarning("[session.jint] scripting API not found – no modules bound.", this);

				var m = JintEngine.PrepareModule(Script.GetContent());
				_engine.AddModule("__main__", x => x.AddModule(m));
				Context = _engine.ImportModule("__main__");
				_initialized = true;

				try {
					var exports = Script.GetExports();
					foreach (var prop in exports)
						SetExports(prop.Key, prop.Value);
				} catch (Exception e) {
					Logger.LogError(e, this);
				}

				Main.CoreAPI.EventAPI.Emit("jint_engine_created", this, _engine);
			} catch (Exception e) {
				_engine = null;
				Logger.LogError(e, this);
			}
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
				if (!_initialized)
					return;

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
				if (!_initialized)
					return null;

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
				if (!_initialized)
					return default;

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
			=> Nox.CCK.Utils.Hash.CRC32(data);

		public static int crc32(string data)
			=> Nox.CCK.Utils.Hash.CRC32(data);

		public static long crc64(byte[] data)
			=> Nox.CCK.Utils.Hash.CRC64(data);

		public static long crc64(string data)
			=> Nox.CCK.Utils.Hash.CRC64(data);
	}
}