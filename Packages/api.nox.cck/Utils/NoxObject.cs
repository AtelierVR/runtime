﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Nox.CCK.Utils {
	/// <summary>
	/// Base interface for Nox objects, providing methods to invoke asynchronous methods via reflection.
	/// </summary>
	public interface INoxObject {
		/// <summary>
		/// Invokes an asynchronous method without returning a value.
		/// </summary>
		/// <param name="method">The name of the method to invoke.</param>
		/// <param name="args">The arguments to pass to the method.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		public async UniTask InvokeAsyncMethod(string method, params object[] args)
			=> await CallAsyncMethod<object>(method, args);

		/// <summary>
		/// Invokes an asynchronous method that returns an INoxObject.
		/// </summary>
		/// <param name="method">The name of the method to invoke.</param>
		/// <param name="args">The arguments to pass to the method.</param>
		/// <returns>A task representing the asynchronous operation with the INoxObject result.</returns>
		public async UniTask<INoxObject> CallAsyncMethod(string method, params object[] args)
			=> await CallAsyncMethod<INoxObject>(method, args);

		private bool WaitCallback(object awaiter) {
			try {
				if (awaiter == null) {
					Logger.LogError("Awaiter is null.");
					return false;
				}

				var isCompletedProperty = awaiter.GetType().GetProperty("IsCompleted");
				if (isCompletedProperty == null) {
					Logger.LogError("IsCompleted property not found on awaiter.");
					return false;
				}

				return (bool)isCompletedProperty.GetValue(awaiter)!;
			} catch (Exception e) {
				Logger.LogError($"Error checking awaiter: {e?.Message}");
				Logger.LogException(e);
				return false;
			} catch {
				Logger.LogError("Unknown error checking awaiter.");
				return false;
			}
		}

		public async UniTask<T> CallAsyncMethod<T>(string method, params object[] args) {
			var type = GetType();
			try {
				var methodInfo = type.GetMethod(method, BindingFlags.Instance | BindingFlags.Public);

				if (methodInfo == null) {
					Logger.LogWarning($"Method (ASYNC CALL) {method} not found in {type}");
					return default;
				}

				var attr = methodInfo.GetCustomAttributes(typeof(NoxPublicAttribute), true);
				if (attr.Length == 0) {
					Logger.LogWarning($"Method (ASYNC CALL) {method} in {type} has no NoxPublicAttribute");
					return default;
				}

				if (!((NoxPublicAttribute)attr[0]).Access.HasFlag(NoxAccess.Execute)) {
					Logger.LogWarning($"Method (ASYNC CALL) {method} in {type} is not executable");
					return default;
				}

				var waiter = methodInfo.Invoke(this, args);

				if (waiter is null)
					return default;

				if (waiter is UniTask<T> task0)
					return await task0;

				if (waiter is Task<T> task1)
					return await task1.AsUniTask();

				if (waiter is T value0)
					return value0;

				if (waiter.GetType().GetGenericTypeDefinition() == typeof(UniTask<object>).GetGenericTypeDefinition()) {
					var awaiter = waiter.GetType().GetMethod("GetAwaiter")?.Invoke(waiter, null);
					if (awaiter != null) {
						await UniTask.WaitUntil(() => WaitCallback(awaiter));
						var result = awaiter.GetType().GetMethod("GetResult")?.Invoke(awaiter, null);

						if (result is null)
							return default;

						if (result is T t)
							return t;

						if (result.GetType().GetInterfaces().Contains(typeof(T)))
							return (T)result;
					}
				}

				Logger.LogWarning(
					$"Method (ASYNC CALL) {method} in {type} returned invalid type {waiter.GetType()} instead of {typeof(T)}"
				);
				return default;
			} catch (Exception e) {
				Logger.LogError($"Error invoking method (ASYNC CALL) {method} in {type}: {e.Message}");
				Logger.LogException(e);
				return default;
			}
		}

		public void InvokeMethod(string method, params object[] args)
			=> CallMethod<object>(method, args);

		public INoxObject CallMethod(string method, params object[] args)
			=> CallMethod<INoxObject>(method, args);

		public virtual T CallMethod<T>(string method, params object[] args) {
			var type = GetType();
			try {
				var methodInfo = type.GetMethod(method, BindingFlags.Instance | BindingFlags.Public);

				if (methodInfo == null) {
					Logger.LogWarning($"Method (CALL) {method} not found in {type}");
					return default;
				}

				var attr = methodInfo.GetCustomAttributes(typeof(NoxPublicAttribute), true);
				if (attr.Length == 0) {
					Logger.LogWarning($"Method (CALL) {method} in {type} has no NoxPublicAttribute");
					return default;
				}

				if (!((NoxPublicAttribute)attr[0]).Access.HasFlag(NoxAccess.Execute)) {
					Logger.LogWarning($"Method (CALL) {method} in {type} is not executable");
					return default;
				}

				var result = methodInfo.Invoke(this, args);

				switch (result) {
					case null:
						return default;
					case T t:
						return t;
					default:
						Logger.LogWarning(
							$"Method (CALL) {method} in {type} returned invalid type {result.GetType()} instead of {typeof(T)}"
						);
						return default;
				}
			} catch (Exception e) {
				Logger.LogError($"Error invoking method (CALL) {method} in {type}: {e.Message}");
				Logger.LogException(e);
				return default;
			}
		}

		public INoxObject GetField(string field)
			=> GetField<INoxObject>(field);

		public virtual T GetField<T>(string field) {
			try {
				var type      = GetType();
				var fieldInfo = type.GetField(field, BindingFlags.Instance | BindingFlags.Public);
				if (fieldInfo == null) {
					Logger.LogWarning($"Field (GET) {field} not found in {type}");
					return default;
				}

				var attr = fieldInfo.GetCustomAttributes(typeof(NoxPublicAttribute), true);
				if (attr.Length == 0) {
					Logger.LogWarning($"Field (GET) {field} in {type} has no NoxPublicAttribute");
					return default;
				}

				if (!((NoxPublicAttribute)attr[0]).Access.HasFlag(NoxAccess.Read)) {
					Logger.LogWarning($"Field (GET) {field} in {type} is not readable");
					return default;
				}

				var result = fieldInfo.GetValue(this);

				if (result is null)
					return default;

				if (typeof(T) == typeof(object))
					return (T)result;

				if (result is T t)
					return t;

				Logger.LogWarning(
					$"Field (GET) {field} in {type} returned invalid type {result.GetType()} instead of {typeof(T)}"
				);

				return default;
			} catch (Exception e) {
				Logger.LogError($"Error getting field (GET) {field} in {GetType()}: {e.Message}");
				Logger.LogException(e);
				return default;
			}
		}

		public virtual void SetField<T>(string field, T value) {
			var type      = GetType();
			var fieldInfo = type.GetField(field, BindingFlags.Instance | BindingFlags.Public);
			if (fieldInfo == null) {
				Logger.LogWarning($"Field (SET) {field} not found in {type}");
				return;
			}

			var attr = fieldInfo.GetCustomAttributes(typeof(NoxPublicAttribute), true);
			if (attr.Length == 0) {
				Logger.LogWarning($"Field (SET) {field} in {type} has no NoxPublicAttribute");
				return;
			}

			if (!((NoxPublicAttribute)attr[0]).Access.HasFlag(NoxAccess.Write)) {
				Logger.LogWarning($"Field (SET) {field} in {type} is not writable");
				return;
			}

			fieldInfo.SetValue(this, value);
		}

		public virtual bool HasMethod(string method) {
			try {
				var type       = GetType();
				var methodInfo = type.GetMethod(method);
				if (methodInfo == null) {
					Logger.LogWarning($"Method (HAS) {method} not found in {type}");
					return false;
				}

				var attr = methodInfo.GetCustomAttributes(typeof(NoxPublicAttribute), true);
				if (attr.Length == 0) {
					Logger.LogWarning($"Method (HAS) {method} in {type} has no NoxPublicAttribute");
					return false;
				}

				if (!((NoxPublicAttribute)attr[0]).Access.HasFlag(NoxAccess.Execute)) {
					Logger.LogWarning($"Method (HAS) {method} in {type} is not executable");
					return false;
				}

				return true;
			} catch (Exception e) {
				Logger.LogError($"Error checking method (HAS) {method} in {GetType()}: {e.Message}");
				Logger.LogException(e);
				return false;
			}
		}

		public virtual bool HasField(string field) {
			try {
				var type      = GetType();
				var fieldInfo = type.GetField(field);
				if (fieldInfo == null) {
					Logger.LogWarning($"Field (HAS) {field} not found in {type}");
					return false;
				}

				var attr = fieldInfo.GetCustomAttributes(typeof(NoxPublicAttribute), true);
				if (attr.Length == 0) {
					Logger.LogWarning($"Field (HAS) {field} in {type} has no NoxPublicAttribute");
					return false;
				}

				if (!((NoxPublicAttribute)attr[0]).Access.HasFlag(NoxAccess.Read)) {
					Logger.LogWarning($"Field (HAS) {field} in {type} is not readable");
					return false;
				}

				return true;
			} catch (Exception e) {
				Logger.LogError($"Error checking field (HAS) {field} in {GetType()}: {e.Message}");
				Logger.LogException(e);
				return false;
			}
		}

		public virtual bool TryCast<T>(out T o) where T : INoxObject {
			if (this is T t) {
				o = t;
				return true;
			}

			o = default;
			return false;
		}

		public virtual string[] GetMethods() {
			var type    = GetType();
			var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
			var result  = new List<string>();
			foreach (var method in methods) {
				var attr = method.GetCustomAttributes(typeof(NoxPublicAttribute), true);
				if (attr.Length == 0) continue;
				if (!((NoxPublicAttribute)attr[0]).Access.HasFlag(NoxAccess.Execute)) continue;
				result.Add(method.Name);
			}

			return result.ToArray();
		}

		public virtual string[] GetFields() {
			var type   = GetType();
			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
			var result = new List<string>();
			foreach (var field in fields) {
				var attr = field.GetCustomAttributes(typeof(NoxPublicAttribute), true);
				if (attr.Length == 0) continue;
				if (!((NoxPublicAttribute)attr[0]).Access.HasFlag(NoxAccess.Read)) continue;
				result.Add(field.Name);
			}

			return result.ToArray();
		}

		public static INoxObject Cast(object obj)
		{
			if (obj is INoxObject noxObj)
				return noxObj;
			return null;
		}
	}

	[Flags]
	public enum NoxAccess : byte {
		None = 0,

		Read    = 1,
		Write   = 2,
		Execute = 4,

		All    = Read | Write | Execute,
		Field  = Read | Write,
		Method = Read | Execute
	}

	public class NoxPublicAttribute : Attribute {
		public NoxAccess Access { get; }

		public NoxPublicAttribute(NoxAccess access) {
			Access = access;
		}
	}
}