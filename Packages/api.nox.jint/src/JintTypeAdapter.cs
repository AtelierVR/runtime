using System;
using System.Linq;
using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Nox.CCK.Scripting;
using Nox.Jint;
using Nox.Scripting;
using JintEngine = Jint.Engine;
using NoxLogger = Nox.CCK.Utils.Logger;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Esprima.Ast;
using Jint.Native.Array;
using Nox.CCK.Utils;

namespace api.nox.jint {
	/// <summary>
	/// Builds Jint <see cref="ObjectInstance"/> wrappers from
	/// <see cref="IScriptingTypeConverter.Bindings"/> declarations.
	///
	/// Called by <see cref="JintScriptingContext.ToScript"/> when a converter
	/// declares per-instance bindings.
	/// </summary>
	public static class JintTypeAdapter {
		/// <summary>
		/// Create a JS object that exposes all <see cref="IScriptingTypeConverter.Bindings"/>
		/// of <paramref name="converter"/> bound to <paramref name="instance"/>.
		/// </summary>
		public static ObjectInstance BuildInstance(
			JintEngine              engine,
			IScriptingTypeConverter converter,
			object                  instance,
			IJintScriptingContext   ctx
		) {
			// Use the Realm intrinsic directly to avoid engine.Evaluate("Object")
			// (which re-parses via Esprima on every call).
			var obj = engine.Realm.Intrinsics.Object.Construct(
				Array.Empty<JsValue>(),
				engine.Realm.Intrinsics.Object
			);

			// Set Name property for better debugging (e.g. console.log(obj) shows the type name instead of "Object").
			obj.DefineOwnProperty(
				"Name",
				new PropertyDescriptor(
					converter.HandledType.Name,
					writable: false,
					enumerable: false,
					configurable: false
				)
			);

			try {
				foreach (var binding in converter.Bindings) {
					var name = binding.Name.Resolve(NameResolver.camelCaseStyle);

					switch (binding) {
						case IScriptingTypeProperty property: {
							if (property.Setter == null || property.Flags.HasFlag(ScriptingTypePropertyFlags.IsReadOnly)) {
								var getter = new GetterFunctionInstance(
									engine,
									_ => ToValue(engine, property.Getter(ctx, instance), ctx)
								);

								obj.DefineOwnProperty(
									name,
									new GetSetPropertyDescriptor(getter, null, enumerable: true, configurable: false)
								);
							} else {
								// Read-write accessor.
								var getter = new GetterFunctionInstance(
									engine,
									_ => ToValue(engine, property.Getter(ctx, instance), ctx)
								);

								var setter = new SetterFunctionInstance(
									engine,
									(_, v) => property.Setter(ctx, instance, FromJsValue(v))
								);

								obj.DefineOwnProperty(
									name,
									new GetSetPropertyDescriptor(getter, setter, enumerable: true, configurable: true)
								);
							}
							break;
						}

						case IScriptingTypeSyncMethod method: {
							var fn = new ClrFunctionInstance(
								engine,
								name,
								(_, args) => {
									var nativeArgs = args.Select(FromJsValue).ToArray();
									try {
										var result = method.Handler(ctx, instance, nativeArgs);
										return ToValue(engine, result, ctx);
									} catch (Exception e) {
										NoxLogger.LogError(
											$"[scripting] {converter.HandledType.Name}.{name}: {e.Message}");
										return JsValue.Null;
									}
								});

							obj.DefineOwnProperty(
								name,
								new PropertyDescriptor(fn, writable: true, enumerable: true, configurable: true)
							);
							break;
						}

						case IScriptingTypeAsyncMethod method: {
							var fn = new ClrFunctionInstance(
								engine,
								name,
								(_, args) => {
									var          nativeArgs = args.Select(FromJsValue).ToArray();
									Task<object> task;
									try {
										task = method.Handler(ctx, instance, nativeArgs);
									} catch (Exception e) {
										NoxLogger.LogError(
											$"[scripting] {converter.HandledType.Name}.{name}: {e.Message}");
										return JsValue.Null;
									}
									return ToValue(engine, task, ctx);
								});

							obj.DefineOwnProperty(
								name,
								new PropertyDescriptor(fn, writable: true, enumerable: true, configurable: true)
							);
							break;
						}
					}
				}
			} catch (Exception e) {
				NoxLogger.LogError($"[scripting] BuildObject({converter.HandledType.Name}): {e.Message}");
			}

			return obj;
		}

		public static void BindModule(
			JintEngine                 engine,
			ModuleBuilder              builder,
			IScriptingModuleDefinition module,
			IJintScriptingContext      ctx
		) {
			try {
				foreach (var binding in module.Bindings) {

					switch (binding) {
						case IScriptingPropertyDefinition property: {
							var name = binding.Name.Resolve(NameResolver.camelCaseStyle);
							builder.ExportValue(name, ToValue(engine, property.Getter(ctx), ctx));
							break;
						}

						case IScriptingSyncMethodDefinition method: {
							var name = binding.Name.Resolve(NameResolver.camelCaseStyle);
							builder.ExportFunction(name, (args) => {
								var nativeArgs = args.Select(FromJsValue).ToArray();
								try {
									return ToValue(engine, method.Handler(ctx, nativeArgs), ctx);
								} catch (Exception e) {
									NoxLogger.LogError($"[scripting] {module.Id.Resolve(NameResolver.snake_case_style)}.{name}: {e.Message}");
									return JsValue.Null;
								}
							});
							break;
						}

						case IScriptingAsyncMethodDefinition method: {
							var name = binding.Name.Resolve(NameResolver.camelCaseStyle);
							builder.ExportFunction(name, (args) => {
								var          nativeArgs = args.Select(FromJsValue).ToArray();
								Task<object> task;
								try {
									task = method.Handler(ctx, nativeArgs);
								} catch (Exception e) {
									NoxLogger.LogError($"[scripting] {module.Id.Resolve(NameResolver.snake_case_style)}.{name}: {e.Message}");
									return JsValue.Null;
								}
								return ToValue(engine, task, ctx);
							});

							break;
						}

						case IScriptingTypeConverterDefinition typeDef: {
							var name = binding.Name.Resolve(NameResolver.PascalCaseStyle);
							builder.ExportValue(name, BuildType(engine, typeDef.Converter, ctx));
							break;
						}
					}
				}
			} catch (Exception e) {
				NoxLogger.LogError($"[scripting] BuildModule({module.Id.Resolve(NameResolver.snake_case_style)}): {e.Message}");
			}
		}



		public static ObjectInstance BuildType(
			JintEngine              engine,
			IScriptingTypeConverter converter,
			IJintScriptingContext   ctx
		) {
			var obj = engine.Realm.Intrinsics.Object.Construct(
				Array.Empty<JsValue>(),
				engine.Realm.Intrinsics.Object
			);

			// Set Name property for better debugging (e.g. console.log(obj) shows the type name instead of "Object").
			obj.DefineOwnProperty(
				"Name",
				new PropertyDescriptor(
					converter.HandledType.Name,
					writable: false,
					enumerable: false,
					configurable: false
				)
			);

			try {
				// Setup constructor if declared, allowing scripts to create new instances via `new Type(args)`.
				if (converter.Constructor != null) {
					var constructorFn = new ClrFunctionInstance(engine, converter.HandledType.Name,
						(_, args) => {
							try {
								return ToValue(engine, converter.Constructor(ctx, args.Select(FromJsValue).ToArray()), ctx);
							} catch (Exception e) {
								NoxLogger.LogError(
									$"[scripting] {converter.HandledType.Name} constructor: {e.Message}");
								return JsValue.Null;
							}
						});

					// allow by new Type(args)
					obj.DefineOwnProperty(
						converter.HandledType.Name,
						new PropertyDescriptor(constructorFn, writable: true, enumerable: false, configurable: true)
					);

					// allow by Type.from(args)
					obj.DefineOwnProperty(
						"from",
						new PropertyDescriptor(constructorFn, writable: true, enumerable: false, configurable: true)
					);
				}

				// Setup static bindings (e.g. Type.staticMethod(), Type.staticValue).
				foreach (var binding in converter.StaticBindings) {
					var name = binding.Name.Resolve(NameResolver.camelCaseStyle);

					switch (binding) {
						case IScriptingTypeProperty property:
							if (property.Setter == null || property.Flags.HasFlag(ScriptingTypePropertyFlags.IsReadOnly)) {
								var getter = new GetterFunctionInstance(
									engine,
									_ => ToValue(engine, property.Getter(ctx, null), ctx)
								);

								obj.DefineOwnProperty(
									name,
									new GetSetPropertyDescriptor(getter, null, enumerable: true, configurable: false)
								);
							} else {
								var getter = new GetterFunctionInstance(
									engine,
									_ => ToValue(engine, property.Getter(ctx, null), ctx)
								);

								var setter = new SetterFunctionInstance(
									engine,
									(_, v) => property.Setter(ctx, null, FromJsValue(v))
								);

								obj.DefineOwnProperty(
									name,
									new GetSetPropertyDescriptor(getter, setter, enumerable: true, configurable: true)
								);
							}
							break;

						case IScriptingTypeSyncMethod method:
							var fn = new ClrFunctionInstance(
								engine,
								name,
								(_, args) => {
									var nativeArgs = args.Select(FromJsValue).ToArray();
									try {
										return ToValue(engine, method.Handler(ctx, null, nativeArgs), ctx);
									} catch (Exception e) {
										NoxLogger.LogError(
											$"[scripting] {converter.HandledType.Name}.{name}: {e.Message}");
										return JsValue.Null;
									}
								});

							obj.DefineOwnProperty(
								name,
								new PropertyDescriptor(fn, writable: true, enumerable: true, configurable: true)
							);
							break;

						case IScriptingTypeAsyncMethod method:
							var asyncFn = new ClrFunctionInstance(
								engine,
								name,
								(_, args) => {
									var          nativeArgs = args.Select(FromJsValue).ToArray();
									Task<object> task;
									try {
										task = method.Handler(ctx, null, nativeArgs);
									} catch (Exception e) {
										NoxLogger.LogError(
											$"[scripting] {converter.HandledType.Name}.{name}: {e.Message}");
										return JsValue.Null;
									}
									return ToValue(engine, task, ctx);
								});

							obj.DefineOwnProperty(
								name,
								new PropertyDescriptor(asyncFn, writable: true, enumerable: true, configurable: true)
							);
							break;
					}
				}
			} catch (Exception e) {
				NoxLogger.LogError($"[scripting] BuildType({converter.HandledType.Name}): {e.Message}");
			}

			return obj;
		}

		private static JsValue ToArray(JintEngine engine, Array list, IJintScriptingContext context = null) {
			if (list.Length == 0)
				return engine.Realm.Intrinsics.Array.Construct(0);
			var arr = engine.Realm.Intrinsics.Array.Construct(list.Length);
			for (var i = 0; i < list.Length; i++)
				arr[(uint)i] = ToValue(engine, list.GetValue(i), context);
			return arr;
		}

		public static JsValue ToValue(JintEngine engine, object value, IJintScriptingContext context = null)
			=> value switch {
				JsValue v                      => v,
				bool b                         => b ? JsBoolean.True : JsBoolean.False,
				null                           => JsValue.Null,
				Task<object> t                 => ToPromise(engine, t.AsUniTask(), context),
				UniTask<object> t              => ToPromise(engine, t, context),
				_ when value.GetType().IsArray => ToArray(engine, (Array)value, context),
				_                              => JsValue.FromObject(engine, value)
			};

		static internal JsValue ToPromise(JintEngine engine, UniTask<object> task, IJintScriptingContext context = null) {
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

			task.Then(
				onSuccess: v => engine.Invoke(resolve!, ToValue(engine, v, context)),
				onError: ex => {
					NoxLogger.LogError($"[scripting] async method failed: {ex.Message}");
					engine.Invoke(reject!, ToValue(engine, ex, context));
				}
			).Forget();

			return promise;
		}

		/// <summary>Convert a <see cref="JsValue"/> to a plain C# object for handler arguments.
		/// Plain JS objects (non-array ObjectInstances) are returned as <see cref="ObjectInstance"/>
		/// so that type converters can pattern-match on them.</summary>
		public static object FromJsValue(JsValue value) {
			if (value.IsNull() || value.IsUndefined())
				return null;

			if (!value.IsObject())
				return value.ToObject();

			var obj = value.AsObject();

			if (obj is ArrayInstance arr) {
				var len    = (int)arr.Length;
				var result = new object[ len ];
				for (var i = 0; i < len; i++)
					result[i] = FromJsValue(arr[(uint)i]);
				return result;
			}

			if (obj is ObjectWrapper wrapper)
				return wrapper.Target;

			return obj;
		}
	}
}