using System;
using System.Linq;
using System.Threading.Tasks;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Nox.CCK.Scripting;
using Nox.Jint;
using Nox.Scripting;
using JintEngine = Jint.Engine;
using NoxLogger = Nox.CCK.Utils.Logger;

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
		public static JsValue BuildObject(
			JintEngine              engine,
			IScriptingTypeConverter converter,
			object                  instance,
			IJintScriptingContext   context) {
			var obj = engine.Construct("Object", System.Array.Empty<JsValue>());

			foreach (var binding in converter.Bindings) {
				var name = binding.Name.Resolve(NameResolver.camelCaseStyle);

				if (binding is IScriptingTypeProperty prop) {
					var capturedProp     = prop;
					var capturedInstance = instance;

					if (prop.IsReadOnly) {
						// Read-only: snapshot the value once at conversion time
						var val = JintModuleAdapter.ToJsValue(engine, prop.Getter(context, instance), context);
						obj.DefineOwnProperty(name,
							new PropertyDescriptor(val, writable: false, enumerable: true, configurable: false));
					} else {
						// Live accessor: backend reads/writes through getter/setter
						var getter = new GetterFunctionInstance(engine,
							_ => JintModuleAdapter.ToJsValue(engine, capturedProp.Getter(context, capturedInstance), context));
						var setter = new SetterFunctionInstance(engine,
							(_, v) => capturedProp.Setter(context, capturedInstance, JintModuleAdapter.FromJsValue(v)));
						obj.DefineOwnProperty(name,
							new GetSetPropertyDescriptor(getter, setter, enumerable: true, configurable: true));
					}
				} else if (binding is IScriptingTypeSyncMethod sync) {
					var capturedSync     = sync;
					var capturedInstance = instance;
					var fn = new ClrFunctionInstance(engine, name,
						(_, args) => {
							var nativeArgs = args.Select(JintModuleAdapter.FromJsValue).ToArray();
							try {
								var result = capturedSync.Handler(context, capturedInstance, nativeArgs);
								return JintModuleAdapter.ToJsValue(engine, result, context);
							} catch (Exception e) {
								NoxLogger.LogError(
									$"[scripting] {converter.HandledType.Name}.{name}: {e.Message}");
								return JsValue.Null;
							}
						}, 0, PropertyFlag.AllForbidden);
					obj.DefineOwnProperty(name,
						new PropertyDescriptor(fn, writable: true, enumerable: true, configurable: true));
				} else if (binding is IScriptingTypeAsyncMethod async_) {
					var capturedAsync    = async_;
					var capturedInstance = instance;
					var fn = new ClrFunctionInstance(engine, name,
						(_, args) => {
							var nativeArgs = args.Select(JintModuleAdapter.FromJsValue).ToArray();
							Task<object> task;
							try {
								task = capturedAsync.Handler(context, capturedInstance, nativeArgs);
							} catch (Exception e) {
								NoxLogger.LogError(
									$"[scripting] {converter.HandledType.Name}.{name}: {e.Message}");
								return JsValue.Null;
							}
							return JintModuleAdapter.WrapInPromise(engine, task, context);
						}, 0, PropertyFlag.AllForbidden);
					obj.DefineOwnProperty(name,
						new PropertyDescriptor(fn, writable: true, enumerable: true, configurable: true));
				}
			}

			return obj;
		}
	}
}
