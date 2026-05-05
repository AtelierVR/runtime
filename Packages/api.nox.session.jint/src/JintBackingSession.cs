using Jint.Native.Object;
using UnityEngine;
using Jint;
using System;
using System.Collections.Generic;
using System.Linq;
using Jint.Native;
using Jint.Runtime.Modules;
using Nox.Jint;
using Nox.Players;
using JintEngine = Jint.Engine;
using Logger = Nox.CCK.Utils.Logger;
using api.nox.jint;
using Jint.Runtime.Interop;
using Nox.CCK.Scripting;

namespace api.nox.session.jint {
	public class JintBackingSession : MonoBehaviour, IJintBacking {
		public JintBackingModule module;
		public IJintScript Script;
		public ObjectInstance Context;

		/// <summary>
		/// Tags that identify the context this backing runs in (e.g. <c>"session"</c>, <c>"avatar"</c>).
		/// Only modules whose <c>Tags</c> list is empty or shares at least one tag with this list are bound.
		/// </summary>
		public string[] Tags = { "session" };

		private bool _initialized;
		private JintScriptingContext _context;

		/// <summary>Expose the underlying engine for <see cref="JintScriptingContext"/>.</summary>
		internal JintEngine Engine { get; private set; }

		public void Initialize() {
			if (_initialized)
				return;

			try {
				Engine = new JintEngine(
					ctx => {
						ctx.LimitMemory(4_194_304);
						ctx.LimitRecursion(1024);
						ctx.EnableModules(new DefaultModuleLoader(Main.JintAPI.GetModulesPath()));
					}
				);

				var registry = Main.ScriptingAPI;
				_context = new JintScriptingContext(this, registry);

				foreach (var definition in registry.Converters) 
					Engine.SetValue(
						definition.HandledType.Name,
						JintTypeAdapter.BuildType(Engine, definition, _context)
					);

				foreach (var definition in registry.Modules) 
					if (JintModuleAdapter.ModuleMatchesTags(definition, Tags))
						Engine.AddModule(
							definition.Id.Resolve(NameResolver.snake_case_style),
							x => JintTypeAdapter.BindModule(Engine, x, definition, _context)
						);

				var m = JintEngine.PrepareModule(Script.GetContent());
				Engine.AddModule("__main__", x => x.AddModule(m));
				Context      = Engine.ImportModule("__main__");
				_initialized = true;

				try {
					var exports = Script.GetExports();
					foreach (var prop in exports)
						SetExports(prop.Key, prop.Value);
				} catch (Exception e) {
					Logger.LogError(e, this);
				}

				Main.CoreAPI.EventAPI.Emit("jint_engine_created", this, Engine);
			} catch (Exception e) {
				Engine = null;
				Logger.LogError(e, this);
			}
		}

		private void SetExports(string property, object value) {
			try {
				if (Engine == null || Context == null)
					return;
				var export = Context.Get("exports");
				if (export.IsUndefined())
					export = new ObjectWrapper(Engine, new Dictionary<string, object>());
				if (!export.IsObject())
					return;
				var obj   = export.AsObject();
				var jsVal = JintTypeAdapter.ToValue(Engine, value, _context);
				obj.Set(property, jsVal, true);
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

				Engine.Invoke(methodRef, args);
			} catch (Jint.Runtime.JavaScriptException jsEx) {
				Logger.LogError(
					$"[script] {method}(): {jsEx.Message}\n" +
					$"  at {jsEx.Location.Source} line {jsEx.Location.Start.Line} col {jsEx.Location.Start.Column}",
					this);
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
						var jsArray = Engine.Realm.Intrinsics.Array.Construct(bytes.Length);
						for (var j = 0; j < bytes.Length; j++) {
							jsArray[(uint)j] = JsValue.FromObject(Engine, bytes[j]);
						}
						jsArgs[i] = jsArray;
					} else if (args[i] is IPlayer player) {
						jsArgs[i] = new ObjectWrapper(Engine, player);
					} else {
						jsArgs[i] = JsValue.FromObject(Engine, args[i]);
					}
				}

				return Engine.Invoke(methodRef, jsArgs);
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
						var jsArray = Engine.Realm.Intrinsics.Array.Construct(bytes.Length);
						for (var j = 0; j < bytes.Length; j++) {
							jsArray[(uint)j] = JsValue.FromObject(Engine, bytes[j]);
						}
						jsArgs[i] = jsArray;
					} else if (args[i] is IPlayer player) {
						jsArgs[i] = new ObjectWrapper(Engine, player);
					} else {
						jsArgs[i] = JsValue.FromObject(Engine, args[i]);
					}
				}

				var result = Engine.Invoke(methodRef, jsArgs);
				return (T)result.ToObject();
			} catch (Exception e) {
				Logger.LogError(new Exception($"Error invoking method '{method}'", e), this);
				return default;
			}
		}

		private void OnDestroy() {
			if (Engine == null)
				return;
			Main.CoreAPI.EventAPI.Emit("jint_engine_destroyed", this, Engine);
			Engine.Dispose();
			Engine  = null;
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