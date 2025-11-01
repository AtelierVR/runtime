using System;
using System.Globalization;
using Nox.Avatars.Parameters;
using Nox.CCK.Network;
using UnityEngine;
using Buffer = System.Buffer;

namespace Nox.CCK.Avatars.Parameters {
	public static class ParameterExtensions {
		public static void AddValue(this IParameter parameter, object value) {
			var current = parameter.Get();
			parameter.Set(
				parameter.GetValueType() switch {
					ParameterType.Bool       => current.ToBool() || value.ToBool(),
					ParameterType.Byte       => current.ToByte()   + value.ToByte(),
					ParameterType.Short      => current.ToShort()  + value.ToShort(),
					ParameterType.UShort     => current.ToUShort() + value.ToUShort(),
					ParameterType.Int        => current.ToInt()    + value.ToInt(),
					ParameterType.UInt       => current.ToUInt()   + value.ToUInt(),
					ParameterType.Long       => current.ToLong()   + value.ToLong(),
					ParameterType.ULong      => current.ToULong()  + value.ToULong(),
					ParameterType.Float      => current.ToFloat()  + value.ToFloat(),
					ParameterType.Double     => current.ToDouble() + value.ToDouble(),
					ParameterType.String     => current.ToString() + value.ToString(),
					ParameterType.ByteArray  => AppendBytes(current as byte[], value as byte[]),
					ParameterType.Vector3    => current.ToVector3() + value.ToVector3(),
					ParameterType.Quaternion => Quaternion.LookRotation(current.ToQuaternion().eulerAngles + value.ToQuaternion().eulerAngles),
					_                        => throw new ArgumentOutOfRangeException()
				}
			);
		}

		public static void SubtractValue(this IParameter parameter, object value) {
			var current = parameter.Get();
			parameter.Set(
				parameter.GetValueType() switch {
					ParameterType.Bool       => current.ToBool() && !value.ToBool(),
					ParameterType.Byte       => current.ToByte()    - value.ToByte(),
					ParameterType.Short      => current.ToShort()   - value.ToShort(),
					ParameterType.UShort     => current.ToUShort()  - value.ToUShort(),
					ParameterType.Int        => current.ToInt()     - value.ToInt(),
					ParameterType.UInt       => current.ToUInt()    - value.ToUInt(),
					ParameterType.Long       => current.ToLong()    - value.ToLong(),
					ParameterType.ULong      => current.ToULong()   - value.ToULong(),
					ParameterType.Float      => current.ToFloat()   - value.ToFloat(),
					ParameterType.Double     => current.ToDouble()  - value.ToDouble(),
					ParameterType.Vector3    => current.ToVector3() - value.ToVector3(),
					ParameterType.ByteArray  => throw new InvalidOperationException("Cannot subtract ByteArray parameters."),
					ParameterType.String     => throw new InvalidOperationException("Cannot subtract String parameters."),
					ParameterType.Quaternion => Quaternion.LookRotation(current.ToQuaternion().eulerAngles - value.ToQuaternion().eulerAngles),
					_                        => throw new ArgumentOutOfRangeException()
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
					ParameterType.ByteArray  => throw new InvalidOperationException("Cannot multiply ByteArray parameters."),
					ParameterType.String     => throw new InvalidOperationException("Cannot multiply String parameters."),
					_                        => throw new ArgumentOutOfRangeException()
				}
			);
		}

		public static void DivideValue(this IParameter parameter, object value) {
			var current = parameter.Get();
			parameter.Set(
				parameter.GetValueType() switch {
					ParameterType.Bool      => current.ToBool() && !value.ToBool(),
					ParameterType.Byte      => current.ToByte()   / value.ToByte(),
					ParameterType.Short     => current.ToShort()  / value.ToShort(),
					ParameterType.UShort    => current.ToUShort() / value.ToUShort(),
					ParameterType.Int       => current.ToInt()    / value.ToInt(),
					ParameterType.UInt      => current.ToUInt()   / value.ToUInt(),
					ParameterType.Long      => current.ToLong()   / value.ToLong(),
					ParameterType.ULong     => current.ToULong()  / value.ToULong(),
					ParameterType.Float     => current.ToFloat()  / value.ToFloat(),
					ParameterType.Double    => current.ToDouble() / value.ToDouble(),
					ParameterType.ByteArray => throw new InvalidOperationException("Cannot divide ByteArray parameters."),
					ParameterType.String    => throw new InvalidOperationException("Cannot divide String parameters."),
					ParameterType.Vector3 => new Vector3(
						current.ToVector3().x / value.ToVector3().x,
						current.ToVector3().y / value.ToVector3().y,
						current.ToVector3().z / value.ToVector3().z
					),
					ParameterType.Quaternion => throw new InvalidOperationException("Cannot divide Quaternion parameters."),
					_                        => throw new ArgumentOutOfRangeException()
				}
			);
		}

		private static byte[] AppendBytes(byte[] a, byte[] b) {
			if (a == null || a.Length == 0) return b ?? Array.Empty<byte>();
			if (b == null || b.Length == 0) return a;

			var result = new byte[a.Length + b.Length];
			Buffer.BlockCopy(a, 0, result, 0, a.Length);
			Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
			return result;
		}
	}
}