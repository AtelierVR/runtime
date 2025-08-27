using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Nox.Avatars.Parameters;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Nox.CCK.Avatars.Parameters;

namespace Nox.CCK.Avatars.StateMachines {
	public class SetParameter : BaseStateMachine {
		public string          key;
		public ParameterType   type;
		public ParameterAction action;
		public byte[]          value;

		public int GetKeyHash()
			=> Animator.StringToHash(key);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetValue<T>(T newValue) {
			if (newValue is null) {
				value = Array.Empty<byte>();
				return;
			}

			(value, type) = newValue switch {
				string str   => (System.Text.Encoding.UTF8.GetBytes(str), ParameterType.String),
				byte[] bytes => (bytes, ParameterType.ByteArray),
				bool b       => (GetBytes(b), ParameterType.Bool),
				byte b       => (new[] { b }, ParameterType.Byte),
				short s      => (GetBytes(s), ParameterType.Short),
				ushort us    => (GetBytes(us), ParameterType.UShort),
				int i        => (GetBytes(i), ParameterType.Int),
				uint ui      => (GetBytes(ui), ParameterType.UInt),
				long l       => (GetBytes(l), ParameterType.Long),
				ulong ul     => (GetBytes(ul), ParameterType.ULong),
				float f      => (GetBytes(f), ParameterType.Float),
				double d     => (GetBytes(d), ParameterType.Double),
				_            => throw new ArgumentException($"Unsupported type: {typeof(T)}")
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T GetValue<T>() {
			if (value == null || value.Length == 0)
				return default;

			return type switch {
				ParameterType.String when typeof(T) == typeof(string)
					=> (T)(object)System.Text.Encoding.UTF8.GetString(value),
				ParameterType.ByteArray when typeof(T) == typeof(byte[])
					=> (T)(object)value,
				ParameterType.Bool when typeof(T) == typeof(bool)
					=> (T)(object)(value[0] != 0),
				ParameterType.Byte when typeof(T) == typeof(byte)
					=> (T)(object)value[0],
				ParameterType.Short when typeof(T) == typeof(short)
					=> (T)(object)MemoryMarshal.Read<short>(value),
				ParameterType.UShort when typeof(T) == typeof(ushort)
					=> (T)(object)MemoryMarshal.Read<ushort>(value),
				ParameterType.Int when typeof(T) == typeof(int)
					=> (T)(object)MemoryMarshal.Read<int>(value),
				ParameterType.UInt when typeof(T) == typeof(uint)
					=> (T)(object)MemoryMarshal.Read<uint>(value),
				ParameterType.Long when typeof(T) == typeof(long)
					=> (T)(object)MemoryMarshal.Read<long>(value),
				ParameterType.ULong when typeof(T) == typeof(ulong)
					=> (T)(object)MemoryMarshal.Read<ulong>(value),
				ParameterType.Float when typeof(T) == typeof(float)
					=> (T)(object)MemoryMarshal.Read<float>(value),
				ParameterType.Double when typeof(T) == typeof(double)
					=> (T)(object)MemoryMarshal.Read<double>(value),
				ParameterType.Vector3 when typeof(T) == typeof(Vector3)
					=> (T)(object)MemoryMarshal.Read<Vector3>(value),
				ParameterType.Quaternion when typeof(T) == typeof(Quaternion)
					=> (T)(object)MemoryMarshal.Read<Quaternion>(value),
				_ => throw new InvalidOperationException($"Cannot convert {type} to {typeof(T)}")
			};
		}

		public object GetValue()
			=> type switch {
				ParameterType.String     => GetValue<string>(),
				ParameterType.ByteArray  => GetValue<byte[]>(),
				ParameterType.Bool       => GetValue<bool>(),
				ParameterType.Byte       => GetValue<byte>(),
				ParameterType.Short      => GetValue<short>(),
				ParameterType.UShort     => GetValue<ushort>(),
				ParameterType.Int        => GetValue<int>(),
				ParameterType.UInt       => GetValue<uint>(),
				ParameterType.Long       => GetValue<long>(),
				ParameterType.ULong      => GetValue<ulong>(),
				ParameterType.Float      => GetValue<float>(),
				ParameterType.Double     => GetValue<double>(),
				ParameterType.Vector3    => GetValue<Vector3>(),
				ParameterType.Quaternion => GetValue<Quaternion>(),
				_                        => null
			};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] GetBytes(bool value) {
			return new[] { value ? (byte)1 : (byte)0 };
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] GetBytes(short value) {
			var bytes = new byte[sizeof(short)];
			MemoryMarshal.Write(bytes, ref value);
			return bytes;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] GetBytes(ushort value) {
			var bytes = new byte[sizeof(ushort)];
			MemoryMarshal.Write(bytes, ref value);
			return bytes;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] GetBytes(int value) {
			var bytes = new byte[sizeof(int)];
			MemoryMarshal.Write(bytes, ref value);
			return bytes;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] GetBytes(uint value) {
			var bytes = new byte[sizeof(uint)];
			MemoryMarshal.Write(bytes, ref value);
			return bytes;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] GetBytes(long value) {
			var bytes = new byte[sizeof(long)];
			MemoryMarshal.Write(bytes, ref value);
			return bytes;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] GetBytes(ulong value) {
			var bytes = new byte[sizeof(ulong)];
			MemoryMarshal.Write(bytes, ref value);
			return bytes;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] GetBytes(float value) {
			var bytes = new byte[sizeof(float)];
			MemoryMarshal.Write(bytes, ref value);
			return bytes;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] GetBytes(double value) {
			var bytes = new byte[sizeof(double)];
			MemoryMarshal.Write(bytes, ref value);
			return bytes;
		}


		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
			base.OnStateEnter(animator, stateInfo, layerIndex);

			var module = RuntimeAvatar?.GetDescriptor()
				.GetModules<IParameterModule>()
				.FirstOrDefault();

			if (module == null) {
				Logger.LogWarning("No ParameterModule found on avatar, the parameter won't be set.");
				return;
			}

			var parameter = module.GetParameter(GetKeyHash());
			if (parameter == null) {
				Logger.LogWarning($"Parameter '{key}' not found on avatar.");
				return;
			}

			try {
				switch (action) {
					case ParameterAction.Assign:
						parameter.Set(GetValue());
						break;
					case ParameterAction.Add:
						parameter.AddValue(GetValue());
						break;
					case ParameterAction.Subtract:
						parameter.SubtractValue(GetValue());
						break;
					case ParameterAction.Multiply:
						parameter.MultiplyValue(GetValue());
						break;
					case ParameterAction.Divide:
						parameter.DivideValue(GetValue());
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			} catch (Exception e) {
				Logger.LogError($"Failed to set parameter '{key}': {e.Message}");
			}
		}
	}
}