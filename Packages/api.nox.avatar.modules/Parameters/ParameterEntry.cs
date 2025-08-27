using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Nox.Avatars.Parameters;
using UnityEngine;

namespace Nox.CCK.Avatars.Parameters {
	[Serializable]
	public class ParameterEntry {
		public string        name;
		public ParameterType type;
		public byte[]        defaultValue;
		public bool          synced;
		public bool          savable;

		public int GetNameHash()
			=> Animator.StringToHash(name);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetDefaultValue<T>(T newValue) {
			if (newValue is null) {
				defaultValue = Array.Empty<byte>();
				return;
			}

			// Pattern matching optimisé avec allocation réduite
			(defaultValue, type) = newValue switch {
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
		public T GetDefaultValue<T>() {
			if (defaultValue == null || defaultValue.Length == 0)
				return default;

			return type switch {
				ParameterType.String when typeof(T) == typeof(string)
					=> (T)(object)System.Text.Encoding.UTF8.GetString(defaultValue),
				ParameterType.ByteArray when typeof(T) == typeof(byte[])
					=> (T)(object)defaultValue,
				ParameterType.Bool when typeof(T) == typeof(bool)
					=> (T)(object)(defaultValue[0] != 0),
				ParameterType.Byte when typeof(T) == typeof(byte)
					=> (T)(object)defaultValue[0],
				ParameterType.Short when typeof(T) == typeof(short)
					=> (T)(object)MemoryMarshal.Read<short>(defaultValue),
				ParameterType.UShort when typeof(T) == typeof(ushort)
					=> (T)(object)MemoryMarshal.Read<ushort>(defaultValue),
				ParameterType.Int when typeof(T) == typeof(int)
					=> (T)(object)MemoryMarshal.Read<int>(defaultValue),
				ParameterType.UInt when typeof(T) == typeof(uint)
					=> (T)(object)MemoryMarshal.Read<uint>(defaultValue),
				ParameterType.Long when typeof(T) == typeof(long)
					=> (T)(object)MemoryMarshal.Read<long>(defaultValue),
				ParameterType.ULong when typeof(T) == typeof(ulong)
					=> (T)(object)MemoryMarshal.Read<ulong>(defaultValue),
				ParameterType.Float when typeof(T) == typeof(float)
					=> (T)(object)MemoryMarshal.Read<float>(defaultValue),
				ParameterType.Double when typeof(T) == typeof(double)
					=> (T)(object)MemoryMarshal.Read<double>(defaultValue),
				_ => throw new InvalidOperationException($"Cannot convert {type} to {typeof(T)}")
			};
		}

		// Méthodes optimisées utilisant MemoryMarshal pour de meilleures performances
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
	}
}