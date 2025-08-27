using System;
using System.Globalization;
using Nox.Avatars.Parameters;
using UnityEngine;
using Buffer = System.Buffer;

namespace Nox.CCK.Avatars.Parameters {
	public static class ParameterExtensions {
		public static void AddValue(this IParameter parameter, object value) {
			var current = parameter.Get();
			parameter.Set(
				parameter.GetValueType() switch {
					ParameterType.Bool      => current.ToBool() || value.ToBool(),
					ParameterType.Byte      => current.ToByte()   + value.ToByte(),
					ParameterType.Short     => current.ToShort()  + value.ToShort(),
					ParameterType.UShort    => current.ToUShort() + value.ToUShort(),
					ParameterType.Int       => current.ToInt()    + value.ToInt(),
					ParameterType.UInt      => current.ToUInt()   + value.ToUInt(),
					ParameterType.Long      => current.ToLong()   + value.ToLong(),
					ParameterType.ULong     => current.ToULong()  + value.ToULong(),
					ParameterType.Float     => current.ToFloat()  + value.ToFloat(),
					ParameterType.Double    => current.ToDouble() + value.ToDouble(),
					ParameterType.String    => current.ToStr()    + value.ToStr(),
					ParameterType.ByteArray => AppendBytes(current as byte[], value as byte[]),
					ParameterType.Vector3   => current.ToVector3() + value.ToVector3(),
					_                       => throw new ArgumentOutOfRangeException()
				}
			);
		}

		public static void SubtractValue(this IParameter parameter, object value) {
			var current = parameter.Get();
			parameter.Set(
				parameter.GetValueType() switch {
					ParameterType.Bool    => current.ToBool() && !value.ToBool(),
					ParameterType.Byte    => current.ToByte()    - value.ToByte(),
					ParameterType.Short   => current.ToShort()   - value.ToShort(),
					ParameterType.UShort  => current.ToUShort()  - value.ToUShort(),
					ParameterType.Int     => current.ToInt()     - value.ToInt(),
					ParameterType.UInt    => current.ToUInt()    - value.ToUInt(),
					ParameterType.Long    => current.ToLong()    - value.ToLong(),
					ParameterType.ULong   => current.ToULong()   - value.ToULong(),
					ParameterType.Float   => current.ToFloat()   - value.ToFloat(),
					ParameterType.Double  => current.ToDouble()  - value.ToDouble(),
					ParameterType.Vector3 => current.ToVector3() - value.ToVector3(),
					_                     => throw new ArgumentOutOfRangeException()
				}
			);
		}

		public static void MultiplyValue(this IParameter parameter, object value) {
			var current = parameter.Get();
			parameter.Set(
				parameter.GetValueType() switch {
					ParameterType.Bool       => current.ToBool() && value.ToBool(),
					ParameterType.Byte       => current.ToByte()   * value.ToByte(),
					ParameterType.Short      => current.ToShort()  * value.ToShort(),
					ParameterType.UShort     => current.ToUShort() * value.ToUShort(),
					ParameterType.Int        => current.ToInt()    * value.ToInt(),
					ParameterType.UInt       => current.ToUInt()   * value.ToUInt(),
					ParameterType.Long       => current.ToLong()   * value.ToLong(),
					ParameterType.ULong      => current.ToULong()  * value.ToULong(),
					ParameterType.Float      => current.ToFloat()  * value.ToFloat(),
					ParameterType.Double     => current.ToDouble() * value.ToDouble(),
					ParameterType.Vector3    => Vector3.Scale(current.ToVector3(), value.ToVector3()),
					ParameterType.Quaternion => current.ToQuaternion() * value.ToQuaternion(),
					_                        => throw new ArgumentOutOfRangeException()
				}
			);
		}

		public static void DivideValue(this IParameter parameter, object value) {
			var current = parameter.Get();
			parameter.Set(
				parameter.GetValueType() switch {
					ParameterType.Bool   => current.ToBool() && !value.ToBool(),
					ParameterType.Byte   => current.ToByte()   / value.ToByte(),
					ParameterType.Short  => current.ToShort()  / value.ToShort(),
					ParameterType.UShort => current.ToUShort() / value.ToUShort(),
					ParameterType.Int    => current.ToInt()    / value.ToInt(),
					ParameterType.UInt   => current.ToUInt()   / value.ToUInt(),
					ParameterType.Long   => current.ToLong()   / value.ToLong(),
					ParameterType.ULong  => current.ToULong()  / value.ToULong(),
					ParameterType.Float  => current.ToFloat()  / value.ToFloat(),
					ParameterType.Double => current.ToDouble() / value.ToDouble(),
					_                    => throw new ArgumentOutOfRangeException()
				}
			);
		}


		private static bool ToBool(this object parameter)
			=> parameter switch {
				bool b    => b,
				byte b    => b  != 0,
				short s   => s  != 0,
				ushort us => us != 0,
				long l    => l  != 0,
				ulong ul  => ul != 0,
				int i     => i  != 0,
				uint ui   => ui != 0,
				float f   => !Mathf.Approximately(f, 0f),
				double d  => !Mathf.Approximately((float)d, 0f),
				byte[] b  => b.Length > 0                     && b[0] != 0,
				string s  => bool.TryParse(s, out var result) && result,
				_         => false
			};

		private static byte ToByte(this object parameter)
			=> parameter switch {
				bool b    => b ? (byte)1 : (byte)0,
				byte b    => b,
				short s   => (byte)s,
				ushort us => (byte)us,
				long l    => (byte)l,
				ulong ul  => (byte)ul,
				int i     => (byte)i,
				uint ui   => (byte)ui,
				float f   => (byte)f,
				double d  => (byte)d,
				byte[] b  => b.Length >= 1 ? b[0] : (byte)0,
				string s  => byte.TryParse(s, out var result) ? result : (byte)0,
				_         => 0
			};

		private static int ToInt(this object parameter)
			=> parameter switch {
				bool b    => b ? 1 : 0,
				byte b    => b,
				short s   => s,
				ushort us => us,
				long l    => (int)l,
				ulong ul  => (int)ul,
				int i     => i,
				uint ui   => (int)ui,
				float f   => (int)f,
				double d  => (int)d,
				byte[] b  => b.Length >= 4 ? BitConverter.ToInt32(b, 0) : 0,
				string s  => int.TryParse(s, out var result) ? result : 0,
				_         => 0
			};

		private static float ToFloat(this object parameter)
			=> parameter switch {
				bool b    => b ? 1f : 0f,
				byte b    => b,
				short s   => s,
				ushort us => us,
				long l    => l,
				ulong ul  => ul,
				int i     => i,
				uint ui   => ui,
				float f   => f,
				double d  => (float)d,
				byte[] b  => b.Length >= 4 ? BitConverter.ToSingle(b, 0) : 0f,
				string s  => float.TryParse(s, out var result) ? result : 0f,
				_         => 0f
			};

		private static string ToStr(this object parameter)
			=> parameter switch {
				bool b    => b.ToString(),
				byte b    => b.ToString(),
				short s   => s.ToString(),
				ushort us => us.ToString(),
				long l    => l.ToString(),
				ulong ul  => ul.ToString(),
				int i     => i.ToString(),
				uint ui   => ui.ToString(),
				float f   => f.ToString(CultureInfo.InvariantCulture),
				double d  => d.ToString(CultureInfo.InvariantCulture),
				byte[] b  => System.Text.Encoding.UTF8.GetString(b),
				string s  => s,
				_         => parameter?.ToString() ?? ""
			};

		private static byte[] AppendBytes(byte[] a, byte[] b) {
			if (a == null || a.Length == 0) return b ?? Array.Empty<byte>();
			if (b == null || b.Length == 0) return a;

			var result = new byte[a.Length + b.Length];
			Buffer.BlockCopy(a, 0, result, 0, a.Length);
			Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
			return result;
		}

		private static short ToShort(this object parameter)
			=> parameter switch {
				bool b    => b ? (short)1 : (short)0,
				byte b    => b,
				short s   => s,
				ushort us => (short)us,
				long l    => (short)l,
				ulong ul  => (short)ul,
				int i     => (short)i,
				uint ui   => (short)ui,
				float f   => (short)f,
				double d  => (short)d,
				byte[] b  => b.Length >= 2 ? BitConverter.ToInt16(b, 0) : (short)0,
				string s  => short.TryParse(s, out var result) ? result : (short)0,
				_         => 0
			};

		private static ushort ToUShort(this object parameter)
			=> parameter switch {
				bool b    => b ? (ushort)1 : (ushort)0,
				byte b    => b,
				short s   => (ushort)s,
				ushort us => us,
				long l    => (ushort)l,
				ulong ul  => (ushort)ul,
				int i     => (ushort)i,
				uint ui   => (ushort)ui,
				float f   => (ushort)f,
				double d  => (ushort)d,
				byte[] b  => b.Length >= 2 ? BitConverter.ToUInt16(b, 0) : (ushort)0,
				string s  => ushort.TryParse(s, out var result) ? result : (ushort)0,
				_         => 0
			};

		private static uint ToUInt(this object parameter)
			=> parameter switch {
				bool b    => b ? (uint)1 : (uint)0,
				byte b    => b,
				short s   => (uint)s,
				ushort us => us,
				long l    => (uint)l,
				ulong ul  => (uint)ul,
				int i     => (uint)i,
				uint ui   => ui,
				float f   => (uint)f,
				double d  => (uint)d,
				byte[] b  => b.Length >= 4 ? BitConverter.ToUInt32(b, 0) : (uint)0,
				string s  => uint.TryParse(s, out var result) ? result : (uint)0,
				_         => 0
			};

		private static long ToLong(this object parameter)
			=> parameter switch {
				bool b    => b ? 1L : 0L,
				byte b    => b,
				short s   => s,
				ushort us => us,
				long l    => l,
				ulong ul  => (long)ul,
				int i     => i,
				uint ui   => ui,
				float f   => (long)f,
				double d  => (long)d,
				byte[] b  => b.Length >= 8 ? BitConverter.ToInt64(b, 0) : 0L,
				string s  => long.TryParse(s, out var result) ? result : 0L,
				_         => 0L
			};

		private static ulong ToULong(this object parameter)
			=> parameter switch {
				bool b    => b ? 1UL : 0UL,
				byte b    => b,
				short s   => (ulong)s,
				ushort us => us,
				long l    => (ulong)l,
				ulong ul  => ul,
				int i     => (ulong)i,
				uint ui   => ui,
				float f   => (ulong)f,
				double d  => (ulong)d,
				byte[] b  => b.Length >= 8 ? BitConverter.ToUInt64(b, 0) : 0UL,
				string s  => ulong.TryParse(s, out var result) ? result : 0UL,
				_         => 0UL
			};

		private static double ToDouble(this object parameter)
			=> parameter switch {
				bool b    => b ? 1.0 : 0.0,
				byte b    => b,
				short s   => s,
				ushort us => us,
				long l    => l,
				ulong ul  => ul,
				int i     => i,
				uint ui   => ui,
				float f   => f,
				double d  => d,
				byte[] b  => b.Length >= 8 ? BitConverter.ToDouble(b, 0) : 0.0,
				string s  => double.TryParse(s, out var result) ? result : 0.0,
				_         => 0.0
			};

		private static Vector3 ToVector3(this object parameter)
			=> parameter switch {
				Vector3 v => v,
				byte[] { Length: 12 } b => new Vector3(
					BitConverter.ToSingle(b, 0),
					BitConverter.ToSingle(b, 4),
					BitConverter.ToSingle(b, 8)
				),
				_ => Vector3.zero
			};

		private static Quaternion ToQuaternion(this object parameter)
			=> parameter switch {
				Quaternion q => q,
				byte[] { Length: 16 } b => new Quaternion(
					BitConverter.ToSingle(b, 0),
					BitConverter.ToSingle(b, 4),
					BitConverter.ToSingle(b, 8),
					BitConverter.ToSingle(b, 12)
				),
				_ => Quaternion.identity
			};
	}
}