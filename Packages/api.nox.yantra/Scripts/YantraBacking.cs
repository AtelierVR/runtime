using System;
using System.Collections.Generic;
using Nox.CCK.Build;
using Nox.CCK.YantraJS;
using Nox.YantraJS;
using UnityEngine;
using YantraJS.Core;
using YantraJS.Core.Clr;
using NoxLogger = Nox.CCK.Utils.Logger;

namespace api.nox.yantra {
	[RequireComponent(typeof(YantraScript))]
	public class YantraBacking : MonoBehaviour, IYantraBacking, ICompilable {
		private JSContext    _context;
		private JSValue      _moduleExports;
		private YantraScript _script;
		private bool         _compiled;
		private bool         _initialized;

		public int CompileOrder
			=> 500; // Middle priority

		private void Awake() {
			_script = GetComponent<YantraScript>();
			if (_script) return;
			NoxLogger.LogError("YantraBacking requires a YantraScript component!");
			enabled = false;
		}

		private void Start() {
			if (_compiled) return;
			Compile();
		}

		private void OnDestroy()
			=> _context?.Dispose();

		public void Compile() {
			if (_compiled) return;

			try {
				InitializeContext();
				LoadScript();
				_compiled = true;
			} catch (Exception ex) {
				NoxLogger.LogError($"Failed to compile YantraJS script: {ex.Message}");
				NoxLogger.LogException(ex);
			}
		}

		private void InitializeContext() {
			if (_initialized) return;

			// Create JavaScript context
			_context = new JSContext();

			// Set up global objects and Unity integration
			SetupUnityBindings();

			_initialized = true;
		}

		private void SetupUnityBindings() {
			// Expose this GameObject to JavaScript
			_context["gameObject"] = ClrProxy.From(gameObject);
			_context["transform"]  = ClrProxy.From(transform);
			_context["behaviour"]  = ClrProxy.From(this);

			// Expose Unity classes and constructors - following the documentation pattern
			_context["Vector3"] = new JSFunction(
				(in Arguments a) => {
					var x = a.Length > 0 ? (a[0]?.DoubleValue ?? 0.0) : 0.0;
					var y = a.Length > 1 ? (a[1]?.DoubleValue ?? 0.0) : 0.0;
					var z = a.Length > 2 ? (a[2]?.DoubleValue ?? 0.0) : 0.0;
					return ClrProxy.From(new Vector3((float)x, (float)y, (float)z));
				}
			);

			_context["Quaternion"] = new JSFunction(
				(in Arguments a) => {
					var x = a.Length > 0 ? (a[0]?.DoubleValue ?? 0.0) : 0.0;
					var y = a.Length > 1 ? (a[1]?.DoubleValue ?? 0.0) : 0.0;
					var z = a.Length > 2 ? (a[2]?.DoubleValue ?? 0.0) : 0.0;
					var w = a.Length > 3 ? (a[3]?.DoubleValue ?? 1.0) : 1.0;
					return ClrProxy.From(new Quaternion((float)x, (float)y, (float)z, (float)w));
				}
			);

			// Expose Time utility
			_context["Time"] = ClrProxy.From(typeof(Time));

			// Expose logging functions
			var console = new JSObject {
				["log"] = new JSFunction(
					(in Arguments a) => {
						var message = a.Length > 0 ? a[0]?.ToString() ?? "" : "";
						NoxLogger.Log($"{message}", this, tag: "YantraJS");
						return JSUndefined.Value;
					}
				),
				["warn"] = new JSFunction(
					(in Arguments a) => {
						var message = a.Length > 0 ? a[0]?.ToString() ?? "" : "";
						NoxLogger.LogWarning($"{message}", this, tag: "YantraJS");
						return JSUndefined.Value;
					}
				),
				["error"] = new JSFunction(
					(in Arguments a) => {
						var message = a.Length > 0 ? a[0]?.ToString() ?? "" : "";
						NoxLogger.LogError($"{message}", this, tag: "YantraJS");
						return JSUndefined.Value;
					}
				)
			};

			_context["console"] = console;

			// Global log function for compatibility
			_context["log"] = console["log"];

			// Expose Unity's Application class for common utility methods
			_context["Application"] = ClrProxy.From(typeof(Application));
		}

		private void LoadScript() {
			if (!_script?.asset) {
				NoxLogger.LogWarning("No YantraJS script asset assigned!", this);
				return;
			}

			var scriptContent = _script.GetContent();
			if (string.IsNullOrEmpty(scriptContent)) {
				NoxLogger.LogWarning("YantraJS script content is empty!", this);
				return;
			}

			try {
				// Create exports object for the script
				_context["exports"] = new JSObject();
				var context = new JSContext();

				// create global function
				context["add"] = new JSFunction((in Arguments a) => new JSNumber((a[0]?.IntValue ?? 0) + (a[1]?.IntValue ?? 0)));

				var result = context.Eval("add(4,5)", "script.js");
				NoxLogger.LogDebug($"Result of add(4,5): {result}", this);

				/*
				// Execute the script
				NoxLogger.Log($"Executing YantraJS script:\n{scriptContent}", this);
				var result = _context.Eval(scriptContent);
				*/

				// Store the exports (prefer the result, fallback to context exports)
				_moduleExports = result != JSUndefined.Value
					? result
					: _context["exports"];

				// Update exports in the YantraScript component
				UpdateExports();
			} catch (Exception ex) {
				NoxLogger.LogError($"Failed to execute YantraJS script: {ex.Message}", this);
				NoxLogger.LogException(ex, this);
			}
		}

		private void UpdateExports() {
			if (_moduleExports == null || _moduleExports == JSUndefined.Value) return;

			var exports = new Dictionary<string, object>();

			try {
				// Extract all exports from the exports object
				if (_moduleExports is JSObject jsObj) {
					var propertyNames = jsObj.GetAllKeys();
					while (propertyNames.MoveNext(out var key)) {
						try {
							var keyStr = key?.ToString();
							if (string.IsNullOrEmpty(keyStr)) continue;

							var value = _moduleExports[keyStr];

							// Convert JSValue to appropriate C# type
							var convertedValue = ConvertFromJsValue(value);
							exports[keyStr] = convertedValue;
						} catch (Exception ex) {
							NoxLogger.LogWarning($"Failed to extract export '{key}': {ex.Message}", this);
						}
					}
				}
			} catch (Exception ex) {
				NoxLogger.LogWarning($"Failed to enumerate exports: {ex.Message}", this);
			}

			#if UNITY_EDITOR
			_script.SetExports(exports);
			#endif
		}

		public void Invoke(string method, params object[] args) {
			if (!_compiled || _moduleExports == null || _moduleExports == JSUndefined.Value) {
				NoxLogger.LogWarning($"Cannot invoke '{method}': Script not compiled or no exports available", this);
				return;
			}

			try {
				var function = _moduleExports[method];
				if (function is JSFunction jsFunction) {
					// Convert C# args to JSValues
					var jsArgs = new JSValue[args?.Length ?? 0];
					if (args != null)
						for (var i = 0; i < args.Length; i++)
							jsArgs[i] = ConvertToJsValue(args[i]);

					// Call function with arguments
					jsFunction.Call(JSUndefined.Value, jsArgs);
				} else if (function != JSUndefined.Value && function != JSNull.Value)
					NoxLogger.LogWarning($"Export '{method}' exists but is not a function (type: {function?.GetType().Name})", this);
			} catch (Exception ex) {
				NoxLogger.LogError($"Error invoking '{method}': {ex.Message}", this);
				NoxLogger.LogException(ex, this);
			}
		}

		public object Call(string method, object[] args) {
			if (!_compiled || _moduleExports == null || _moduleExports == JSUndefined.Value) {
				NoxLogger.LogWarning($"Cannot call '{method}': Script not compiled or no exports available", this);
				return null;
			}

			try {
				var function = _moduleExports[method];
				if (function is JSFunction jsFunction) {
					// Convert C# args to JSValues
					var jsArgs = new JSValue[args?.Length ?? 0];
					if (args != null)
						for (var i = 0; i < args.Length; i++)
							jsArgs[i] = ConvertToJsValue(args[i]);

					// Call function with arguments and get result
					var result = jsFunction.Call(JSUndefined.Value, jsArgs);

					return ConvertFromJsValue(result);
				}

				NoxLogger.LogWarning($"Export '{method}' is not a function", this);
				return null;
			} catch (Exception ex) {
				NoxLogger.LogError($"Error calling '{method}': {ex.Message}", this);
				NoxLogger.LogException(ex, this);
				return null;
			}
		}

		public T Call<T>(string method, object[] args) {
			var result = Call(method, args);

			if (result == null) return default(T);

			try {
				if (result is T directResult)
					return directResult;

				// Try to convert the result to the desired type
				return (T)Convert.ChangeType(result, typeof(T));
			} catch (Exception ex) {
				NoxLogger.LogError($"Failed to convert result of '{method}' to type {typeof(T).Name}: {ex.Message}", this);
				return default;
			}
		}

		private static JSValue ConvertToJsValue(object obj) {
			if (obj == null) return JSNull.Value;

			return obj switch {
				string s                    => new JSString(s),
				int i                       => new JSNumber(i),
				float f                     => new JSNumber(f),
				double d                    => new JSNumber(d),
				bool b                      => b ? JSBoolean.True : JSBoolean.False,
				Vector3 v3                  => ClrProxy.From(v3),
				Quaternion q                => ClrProxy.From(q),
				UnityEngine.Object unityObj => ClrProxy.From(unityObj),
				_                           => ClrProxy.From(obj)
			};
		}

		private static object ConvertFromJsValue(JSValue jsValue) {
			if (jsValue == null) return null;

			return jsValue switch {
				JSString jsString => jsString.ToString(),
				JSNumber jsNumber => jsNumber.DoubleValue,
				JSBoolean jsBool  => jsBool.BooleanValue,
				JSNull _          => null,
				JSUndefined _     => null,
				JSFunction jsFunc => jsFunc,
				ClrProxy clrProxy => clrProxy.Target,
				_                 => jsValue?.ToString()
			};
		}
	}
}