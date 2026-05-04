using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jint;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Function;
using Jint.Runtime.Interop;
using Nox.CCK.Scripting;
using Nox.CCK.Utils;
using Nox.Jint;
using Nox.Scripting;
using JintEngine = Jint.Engine;
using NoxLogger = Nox.CCK.Utils.Logger;

namespace api.nox.jint {
	/// <summary>
	/// Adapts <see cref="IScriptingModuleDefinition"/> instances from the
	/// scripting registry into Jint engine modules.
	///
	/// Call <see cref="BindAllModules"/> once per engine to wire up every
	/// currently registered module, or <see cref="BindModule"/> to add a
	/// single module on-demand.
	/// </summary>
	public static class JintModuleAdapter {
		/// <summary>
		/// Bind every module currently registered in <paramref name="registry"/>
		/// into <paramref name="engine"/> using <paramref name="context"/> for
		/// per-instance resolution.
		/// Only modules whose tags match <paramref name="backendTags"/> are bound
		/// (empty tags on either side means no restriction).
		/// </summary>
		public static void BindAllModules(JintEngine engine, IJintScriptingContext context, IScriptingAPI registry, IReadOnlyList<string> backendTags = null) {
			foreach (var module in registry.Modules) {
				if (!ModuleMatchesTags(module, backendTags)) continue;
				BindModule(engine, module, context);
			}
		}

		private static bool ModuleMatchesTags(IScriptingModuleDefinition module, IReadOnlyList<string> backendTags) {
			if (module.Tags.Count == 0)                          return true;
			if (backendTags == null || backendTags.Count == 0)   return true;
			return module.Tags.Any(t => backendTags.Contains(t));
		}

		/// <summary>
		/// Bind a single <paramref name="module"/> into <paramref name="engine"/>.
		/// The module will be importable by scripts via its <see cref="IScriptingModuleDefinition.Id"/>.
		/// </summary>
		public static void BindModule(JintEngine engine, IScriptingModuleDefinition module, IJintScriptingContext context) {
			engine.AddModule(module.Id.Resolve(NameResolver.snake_case_style), builder => {
				foreach (var binding in module.Bindings) {
					switch (binding) {
						case IScriptingSyncMethodDefinition syncMethod: {
							var captured = syncMethod;
							builder.ExportFunction(binding.Name.Resolve(NameResolver.camelCaseStyle), args => {
								var nativeArgs = args.Select(FromJsValue).ToArray();
								try {
									var result = captured.Handler(context, nativeArgs);
									return ToJsValue(engine, result, context);
								} catch (Exception e) {
									NoxLogger.LogError(
										$"[scripting] {module.Id}.{binding.Name}: {e.Message}",
										null
									);
									return JsValue.Null;
								}
							});
							break;
						}

						case IScriptingAsyncMethodDefinition asyncMethod: {
							var captured = asyncMethod;
							builder.ExportFunction(binding.Name.Resolve(NameResolver.camelCaseStyle), args => {
								var          nativeArgs = args.Select(FromJsValue).ToArray();
								Task<object> task;
								try {
									task = captured.Handler(context, nativeArgs);
								} catch (Exception e) {
									NoxLogger.LogError(
										$"[scripting] {module.Id}.{binding.Name}: {e.Message}");
									return JsValue.Null;
								}
								return WrapInPromise(engine, task, context);
							});
							break;
						}

						case IScriptingVariableDefinition variable: {
							var captured = variable;
							var name     = binding.Name.Resolve(NameResolver.camelCaseStyle);
							try {
								var val = captured.Getter(context);
								builder.ExportObject(name, val);
							} catch (Exception e) {
								NoxLogger.LogError(
									$"[scripting] {module.Id}.{binding.Name} (variable): {e.Message}");
							}
							break;
						}

						case IScriptingDefaultDefinition defaultDef: {
							var captured = defaultDef;
							if (captured.Getter != null) {
								try {
									var val = captured.Getter(context);
									builder.ExportObject("default", val);
								} catch (Exception e) {
									NoxLogger.LogError(
										$"[scripting] {module.Id}.default (value): {e.Message}");
								}
							} else if (captured.Handler != null) {
								builder.ExportFunction("default", args => {
									var nativeArgs = args.Select(FromJsValue).ToArray();
									try {
										var result = captured.Handler(context, nativeArgs);
										return ToJsValue(engine, result, context);
									} catch (Exception e) {
										NoxLogger.LogError(
											$"[scripting] {module.Id}.default: {e.Message}");
										return JsValue.Null;
									}
								});
							} else if (captured.AsyncHandler != null) {
								builder.ExportFunction("default", args => {
									var          nativeArgs = args.Select(FromJsValue).ToArray();
									Task<object> task;
									try {
										task = captured.AsyncHandler(context, nativeArgs);
									} catch (Exception e) {
										NoxLogger.LogError(
											$"[scripting] {module.Id}.default: {e.Message}");
										return JsValue.Null;
									}
									return WrapInPromise(engine, task, context);
								});
							}
							break;
						}
					}
				}
			});
		}

		// ── Helpers ──────────────────────────────────────────────────────────

		/// <summary>Convert a <see cref="JsValue"/> to a plain C# object for handler arguments.
		/// Plain JS objects (non-array ObjectInstances) are returned as <see cref="ObjectInstance"/>
		/// so that type converters can pattern-match on them.</summary>
		public static object FromJsValue(JsValue value) {
			if (value.IsNull() || value.IsUndefined())
				return null;
			if (!value.IsObject())
				return value.ToObject();  // primitives: bool, number, string
			var obj = value.AsObject();
			if (obj is ArrayInstance arr) {
				// JS array → object[]
				var len    = (int)arr.Length;
				var result = new object[len];
				for (var i = 0; i < len; i++)
					result[i] = FromJsValue(arr[(uint)i]);
				return result;
			}
			// ObjectWrapper → return the wrapped CLR object; plain JS object → return ObjectInstance
			return obj.ToObject() ?? obj;
		}

		/// <summary>Convert a C# result value back to a <see cref="JsValue"/>.
		/// If <paramref name="context"/> is provided, type converters registered in
		/// the scripting registry are applied first.</summary>
		public static JsValue ToJsValue(JintEngine engine, object value, IJintScriptingContext context = null) {
			if (value == null)      return JsValue.Null;
			if (value is JsValue v) return v;
			if (value is bool b)    return b ? JsBoolean.True : JsBoolean.False;
			if (value is byte[] bytes) return BytesToJsValue(engine, bytes);
			// Apply type converter if registered (returns a new value or the same if no converter)
			if (context != null) {
				var converted = context.ToScript(value);
				if (!ReferenceEquals(converted, value))
					return ToJsValue(engine, converted, null); // avoid infinite recursion
			}
			return JsValue.FromObject(engine, value);
		}

		private static JsValue BytesToJsValue(JintEngine engine, byte[] bytes) {
			if (bytes.Length == 0)
				return engine.Realm.Intrinsics.Array.Construct(0);
			var arr = engine.Realm.Intrinsics.Array.Construct(bytes.Length);
			for (var i = 0; i < bytes.Length; i++)
				arr[(uint)i] = JsValue.FromObject(engine, (int)bytes[i]);
			return arr;
		}
		
		internal static JsValue WrapInPromise(JintEngine engine, Task<object> task, IJintScriptingContext context = null) {
			var factory = engine.Evaluate(
				@"(function() {
					var resolve, reject;
					var p = new Promise(function(res, rej) { resolve = res; reject = rej; });
					return { promise: p, resolve: resolve, reject: reject };
				})()"
			).AsObject();

			var promise = factory.Get("promise");
			var resolve = factory.Get("resolve") as FunctionInstance;
			var reject  = factory.Get("reject") as FunctionInstance;

			task.ContinueWith(t => {
				if (t.IsFaulted || t.IsCanceled) {
					NoxLogger.LogError($"[scripting] async method failed: {t.Exception?.Message}");
					engine.Invoke(reject!, false);
				} else {
					engine.Invoke(resolve!, ToJsValue(engine, t.Result, context));
				}
			});

			return promise;
		}
	}
}