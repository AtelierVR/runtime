using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Nox.CCK.Network {
	public static class Serializer {
		// Helper methods for Big Endian conversion (same as Buffer)
		private static byte[] ToBigEndian(short value) {
			return new[] {
				(byte)(value >> 8),
				(byte)value
			};
		}

		private static byte[] ToBigEndian(ushort value) {
			return new[] {
				(byte)(value >> 8),
				(byte)value
			};
		}

		private static byte[] ToBigEndian(int value) {
			return new[] {
				(byte)(value >> 24),
				(byte)(value >> 16),
				(byte)(value >> 8),
				(byte)value
			};
		}

		private static byte[] ToBigEndian(uint value) {
			return new[] {
				(byte)(value >> 24),
				(byte)(value >> 16),
				(byte)(value >> 8),
				(byte)value
			};
		}

		private static byte[] ToBigEndian(long value) {
			return new[] {
				(byte)(value >> 56),
				(byte)(value >> 48),
				(byte)(value >> 40),
				(byte)(value >> 32),
				(byte)(value >> 24),
				(byte)(value >> 16),
				(byte)(value >> 8),
				(byte)value
			};
		}

		private static byte[] ToBigEndian(ulong value) {
			return new[] {
				(byte)(value >> 56),
				(byte)(value >> 48),
				(byte)(value >> 40),
				(byte)(value >> 32),
				(byte)(value >> 24),
				(byte)(value >> 16),
				(byte)(value >> 8),
				(byte)value
			};
		}

		private static byte[] ToBigEndian(float value) {
			var bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
			return bytes;
		}

		private static byte[] ToBigEndian(double value) {
			var bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
			return bytes;
		}

		private static short FromBigEndianInt16(byte[] data, int offset) {
			return (short)((data[offset] << 8) | data[offset + 1]);
		}

		private static ushort FromBigEndianUInt16(byte[] data, int offset) {
			return (ushort)((data[offset] << 8) | data[offset + 1]);
		}

		private static int FromBigEndianInt32(byte[] data, int offset) {
			return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
		}

		private static uint FromBigEndianUInt32(byte[] data, int offset) {
			return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
		}

		private static long FromBigEndianInt64(byte[] data, int offset) {
			return ((long)data[offset] << 56) | ((long)data[offset + 1] << 48) | ((long)data[offset + 2] << 40) | ((long)data[offset + 3] << 32) | ((long)data[offset + 4] << 24) | ((long)data[offset + 5] << 16) | ((long)data[offset + 6] << 8) | data[offset + 7];
		}

		private static ulong FromBigEndianUInt64(byte[] data, int offset) {
			return ((ulong)data[offset] << 56) | ((ulong)data[offset + 1] << 48) | ((ulong)data[offset + 2] << 40) | ((ulong)data[offset + 3] << 32) | ((ulong)data[offset + 4] << 24) | ((ulong)data[offset + 5] << 16) | ((ulong)data[offset + 6] << 8) | data[offset + 7];
		}

		private static float FromBigEndianSingle(byte[] data, int offset) {
			var bytes = new byte[4];
			Array.Copy(data, offset, bytes, 0, 4);
			if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
			return BitConverter.ToSingle(bytes, 0);
		}

		private static double FromBigEndianDouble(byte[] data, int offset) {
			var bytes = new byte[8];
			Array.Copy(data, offset, bytes, 0, 8);
			if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
			return BitConverter.ToDouble(bytes, 0);
		}

		private static byte[] SerializeCollection(ICollection collection) {
			var items = new List<byte[]>();

			// Add count first
			items.Add(ToBigEndian(collection.Count));

			// Add each item
			foreach (var item in collection) {
				var itemBytes = item.ToBytes();
				items.Add(ToBigEndian(itemBytes.Length)); // Length prefix
				items.Add(itemBytes);
			}

			return CombineBytes(items.ToArray());
		}

		private static byte[] SerializeEnumerable(IEnumerable enumerable) {
			var items = new List<byte[]>();
			var count = 0;

			// Collect all items first to get count
			var itemList = new List<object>();
			foreach (var item in enumerable) {
				itemList.Add(item);
				count++;
			}

			// Add count
			items.Add(ToBigEndian(count));

			// Add each item with length prefix
			foreach (var item in itemList) {
				var itemBytes = item.ToBytes();
				items.Add(ToBigEndian(itemBytes.Length));
				items.Add(itemBytes);
			}

			return CombineBytes(items.ToArray());
		}

		private static byte[] SerializeMatrix4X4(UnityEngine.Matrix4x4 matrix) {
			var bytes = new List<byte[]>();
			for (int i = 0; i < 16; i++) {
				bytes.Add(ToBigEndian(matrix[i]));
			}

			return CombineBytes(bytes.ToArray());
		}

		private static byte[] SerializeAnimationCurve(AnimationCurve curve) {
			var items = new List<byte[]>();
			items.Add(ToBigEndian(curve.keys.Length));

			foreach (var key in curve.keys) {
				items.Add(key.ToBytes());
			}

			return CombineBytes(items.ToArray());
		}

		private static byte[] SerializeDictionary(IDictionary dictionary) {
			var items = new List<byte[]>();

			// Add count first
			items.Add(ToBigEndian(dictionary.Count));

			// Add each key-value pair
			foreach (DictionaryEntry entry in dictionary) {
				var keyBytes   = entry.Key.ToBytes();
				var valueBytes = entry.Value.ToBytes();

				items.Add(ToBigEndian(keyBytes.Length));
				items.Add(keyBytes);
				items.Add(ToBigEndian(valueBytes.Length));
				items.Add(valueBytes);
			}

			return CombineBytes(items.ToArray());
		}

		private static byte[] SerializeException(Exception ex)
			=> CombineBytes(
				new List<byte[]> {
					Encoding.UTF8.GetBytes(ex.GetType().Name),
					Encoding.UTF8.GetBytes(ex.Message    ?? string.Empty),
					Encoding.UTF8.GetBytes(ex.StackTrace ?? string.Empty)
				}.ToArray()
			);

		private static byte[] SerializeObject(object obj) {
			try {
				using var stream    = new MemoryStream();
				var       formatter = new BinaryFormatter();
				#pragma warning disable SYSLIB0011 // Type or member is obsolete
				formatter.Serialize(stream, obj);
				#pragma warning restore SYSLIB0011 // Type or member is obsolete
				return stream.ToArray();
			} catch {
				// Fallback to ToString if binary serialization fails
				return Encoding.UTF8.GetBytes(obj.ToString() ?? string.Empty);
			}
		}

		public static byte[] ToBytes(this object o)
			=> o switch {
				null         => Array.Empty<byte>(),
				byte b       => new[] { b },
				sbyte sb     => new[] { (byte)sb },
				bool bl      => BitConverter.GetBytes(bl),
				short s      => ToBigEndian(s),
				ushort us    => ToBigEndian(us),
				int i        => ToBigEndian(i),
				uint ui      => ToBigEndian(ui),
				long l       => ToBigEndian(l),
				ulong ul     => ToBigEndian(ul),
				float f      => ToBigEndian(f),
				double d     => ToBigEndian(d),
				char c       => BitConverter.GetBytes(c),
				string str   => Encoding.UTF8.GetBytes(str),
				byte[] bytes => bytes,
				// Unity Vector types
				UnityEngine.Vector2 v2 => CombineBytes(
					ToBigEndian(v2.x),
					ToBigEndian(v2.y)
				),
				UnityEngine.Vector3 v3 => CombineBytes(
					ToBigEndian(v3.x),
					ToBigEndian(v3.y),
					ToBigEndian(v3.z)
				),
				UnityEngine.Vector4 v4 => CombineBytes(
					ToBigEndian(v4.x),
					ToBigEndian(v4.y),
					ToBigEndian(v4.z),
					ToBigEndian(v4.w)
				),
				UnityEngine.Quaternion q => CombineBytes(
					ToBigEndian(q.x),
					ToBigEndian(q.y),
					ToBigEndian(q.z),
					ToBigEndian(q.w)
				),
				// Unity Integer Vector types
				Vector2Int v2I => CombineBytes(
					ToBigEndian(v2I.x),
					ToBigEndian(v2I.y)
				),
				Vector3Int v3I => CombineBytes(
					ToBigEndian(v3I.x),
					ToBigEndian(v3I.y),
					ToBigEndian(v3I.z)
				),
				// Unity Color types
				Color col => CombineBytes(
					ToBigEndian(col.r),
					ToBigEndian(col.g),
					ToBigEndian(col.b),
					ToBigEndian(col.a)
				),
				Color32 col32 => new[] { col32.r, col32.g, col32.b, col32.a },
				// Unity Rect/Bounds types
				Rect rect => CombineBytes(
					ToBigEndian(rect.x),
					ToBigEndian(rect.y),
					ToBigEndian(rect.width),
					ToBigEndian(rect.height)
				),
				RectInt rectInt => CombineBytes(
					ToBigEndian(rectInt.x),
					ToBigEndian(rectInt.y),
					ToBigEndian(rectInt.width),
					ToBigEndian(rectInt.height)
				),
				Bounds bounds => CombineBytes(
					ToBigEndian(bounds.center.x),
					ToBigEndian(bounds.center.y),
					ToBigEndian(bounds.center.z),
					ToBigEndian(bounds.size.x),
					ToBigEndian(bounds.size.y),
					ToBigEndian(bounds.size.z)
				),
				BoundsInt boundsInt => CombineBytes(
					ToBigEndian(boundsInt.center.x),
					ToBigEndian(boundsInt.center.y),
					ToBigEndian(boundsInt.center.z),
					ToBigEndian(boundsInt.size.x),
					ToBigEndian(boundsInt.size.y),
					ToBigEndian(boundsInt.size.z)
				),
				// Unity Matrix types
				UnityEngine.Matrix4x4 mat => SerializeMatrix4X4(mat),
				// Unity Animation/Keyframe types
				Keyframe kf => CombineBytes(
					ToBigEndian(kf.time),
					ToBigEndian(kf.value),
					ToBigEndian(kf.inTangent),
					ToBigEndian(kf.outTangent)
				),
				AnimationCurve curve => SerializeAnimationCurve(curve),
				// Unity Physics types
				Ray ray => CombineBytes(
					ToBigEndian(ray.origin.x),
					ToBigEndian(ray.origin.y),
					ToBigEndian(ray.origin.z),
					ToBigEndian(ray.direction.x),
					ToBigEndian(ray.direction.y),
					ToBigEndian(ray.direction.z)
				),
				UnityEngine.Plane plane => CombineBytes(
					ToBigEndian(plane.normal.x),
					ToBigEndian(plane.normal.y),
					ToBigEndian(plane.normal.z),
					ToBigEndian(plane.distance)
				),
				// .NET DateTime/TimeSpan
				DateTime dt => ToBigEndian(dt.ToBinary()),
				TimeSpan ts => ToBigEndian(ts.Ticks),
				DateTimeOffset dto => CombineBytes(
					ToBigEndian(dto.DateTime.ToBinary()),
					ToBigEndian(dto.Offset.Ticks)
				),
				// .NET Guid
				Guid guid => guid.ToByteArray(),
				// .NET Numerics types
				BigInteger bigInt => bigInt.ToByteArray(),
				Complex complex => CombineBytes(
					ToBigEndian(complex.Real),
					ToBigEndian(complex.Imaginary)
				),
				// Decimal (complex type)
				decimal dec => CombineBytes(
					decimal.GetBits(dec).SelectMany(i => ToBigEndian(i)).ToArray()
				),
				// .NET Tuple types (up to 8 items)
				ValueTuple<byte> vt1       => new[] { vt1.Item1 },
				ValueTuple<byte, byte> vt2 => new[] { vt2.Item1, vt2.Item2 },
				ValueTuple<int, int> vt2I => CombineBytes(
					ToBigEndian(vt2I.Item1),
					ToBigEndian(vt2I.Item2)
				),
				// Collections
				IEnumerable<byte> byteEnum => byteEnum.ToArray(),
				IDictionary dict           => SerializeDictionary(dict),
				ICollection collection     => SerializeCollection(collection),
				IEnumerable enumerable     => SerializeEnumerable(enumerable),
				// Enum types
				Enum enumValue => ToBigEndian(Convert.ToInt64(enumValue)),
				// Reflection types (exotic)
				Type type         => Encoding.UTF8.GetBytes(type.AssemblyQualifiedName ?? type.FullName ?? type.Name),
				MethodInfo method => Encoding.UTF8.GetBytes($"{method.DeclaringType?.FullName}::{method.Name}"),
				PropertyInfo prop => Encoding.UTF8.GetBytes($"{prop.DeclaringType?.FullName}::{prop.Name}"),
				FieldInfo field   => Encoding.UTF8.GetBytes($"{field.DeclaringType?.FullName}::{field.Name}"),
				// URI types
				Uri uri => Encoding.UTF8.GetBytes(uri.ToString()),
				// Exception types (exotic but useful for debugging)
				Exception ex => SerializeException(ex),
				// Custom serializable objects (fallback for unknown types)
				_ when o.GetType().IsSerializable => SerializeObject(o),
				_                                 => Array.Empty<byte>()
			};

		public static object FromBytes(this byte[] data, Type type) {
			if (data == null || data.Length == 0) return GetDefaultValue(type);

			if (type == typeof(byte)) return data[0];
			if (type == typeof(sbyte)) return (sbyte)data[0];
			if (type == typeof(bool)) return BitConverter.ToBoolean(data, 0);
			if (type == typeof(short)) return FromBigEndianInt16(data, 0);
			if (type == typeof(ushort)) return FromBigEndianUInt16(data, 0);
			if (type == typeof(int)) return FromBigEndianInt32(data, 0);
			if (type == typeof(uint)) return FromBigEndianUInt32(data, 0);
			if (type == typeof(long)) return FromBigEndianInt64(data, 0);
			if (type == typeof(ulong)) return FromBigEndianUInt64(data, 0);
			if (type == typeof(float)) return FromBigEndianSingle(data, 0);
			if (type == typeof(double)) return FromBigEndianDouble(data, 0);
			if (type == typeof(char)) return BitConverter.ToChar(data, 0);
			if (type == typeof(string)) return Encoding.UTF8.GetString(data);
			if (type == typeof(byte[])) return data;

			// Unity Vector types
			if (type == typeof(UnityEngine.Vector2)) {
				return new UnityEngine.Vector2(
					FromBigEndianSingle(data, 0),
					FromBigEndianSingle(data, 4)
				);
			}

			if (type == typeof(UnityEngine.Vector3)) {
				return new UnityEngine.Vector3(
					FromBigEndianSingle(data, 0),
					FromBigEndianSingle(data, 4),
					FromBigEndianSingle(data, 8)
				);
			}

			if (type == typeof(UnityEngine.Vector4)) {
				return new UnityEngine.Vector4(
					FromBigEndianSingle(data, 0),
					FromBigEndianSingle(data, 4),
					FromBigEndianSingle(data, 8),
					FromBigEndianSingle(data, 12)
				);
			}

			if (type == typeof(UnityEngine.Quaternion)) {
				return new UnityEngine.Quaternion(
					FromBigEndianSingle(data, 0),
					FromBigEndianSingle(data, 4),
					FromBigEndianSingle(data, 8),
					FromBigEndianSingle(data, 12)
				);
			}

			// Unity Integer Vector types
			if (type == typeof(Vector2Int)) {
				return new Vector2Int(
					FromBigEndianInt32(data, 0),
					FromBigEndianInt32(data, 4)
				);
			}

			if (type == typeof(Vector3Int)) {
				return new Vector3Int(
					FromBigEndianInt32(data, 0),
					FromBigEndianInt32(data, 4),
					FromBigEndianInt32(data, 8)
				);
			}

			// Unity Color types
			if (type == typeof(Color)) {
				return new Color(
					FromBigEndianSingle(data, 0),
					FromBigEndianSingle(data, 4),
					FromBigEndianSingle(data, 8),
					FromBigEndianSingle(data, 12)
				);
			}

			if (type == typeof(Color32)) {
				return new Color32(data[0], data[1], data[2], data[3]);
			}

			// Unity Rect/Bounds types
			if (type == typeof(Rect)) {
				return new Rect(
					FromBigEndianSingle(data, 0),
					FromBigEndianSingle(data, 4),
					FromBigEndianSingle(data, 8),
					FromBigEndianSingle(data, 12)
				);
			}

			if (type == typeof(RectInt)) {
				return new RectInt(
					FromBigEndianInt32(data, 0),
					FromBigEndianInt32(data, 4),
					FromBigEndianInt32(data, 8),
					FromBigEndianInt32(data, 12)
				);
			}

			if (type == typeof(Bounds)) {
				var center = new UnityEngine.Vector3(
					FromBigEndianSingle(data, 0),
					FromBigEndianSingle(data, 4),
					FromBigEndianSingle(data, 8)
				);
				var size = new UnityEngine.Vector3(
					FromBigEndianSingle(data, 12),
					FromBigEndianSingle(data, 16),
					FromBigEndianSingle(data, 20)
				);
				return new Bounds(center, size);
			}

			if (type == typeof(BoundsInt)) {
				var center = new Vector3Int(
					FromBigEndianInt32(data, 0),
					FromBigEndianInt32(data, 4),
					FromBigEndianInt32(data, 8)
				);
				var size = new Vector3Int(
					FromBigEndianInt32(data, 12),
					FromBigEndianInt32(data, 16),
					FromBigEndianInt32(data, 20)
				);
				return new BoundsInt(center, size);
			}

			// Unity Matrix types
			if (type == typeof(UnityEngine.Matrix4x4)) {
				var matrix = new UnityEngine.Matrix4x4();
				for (int i = 0; i < 16; i++) {
					matrix[i] = FromBigEndianSingle(data, i * 4);
				}

				return matrix;
			}

			// Unity Animation/Keyframe types
			if (type == typeof(Keyframe)) {
				return new Keyframe(
					FromBigEndianSingle(data, 0),
					FromBigEndianSingle(data, 4),
					FromBigEndianSingle(data, 8),
					FromBigEndianSingle(data, 12)
				);
			}

			if (type == typeof(AnimationCurve)) {
				return DeserializeAnimationCurve(data);
			}

			// Unity Physics types
			if (type == typeof(Ray)) {
				var origin = new UnityEngine.Vector3(
					FromBigEndianSingle(data, 0),
					FromBigEndianSingle(data, 4),
					FromBigEndianSingle(data, 8)
				);
				var direction = new UnityEngine.Vector3(
					FromBigEndianSingle(data, 12),
					FromBigEndianSingle(data, 16),
					FromBigEndianSingle(data, 20)
				);
				return new Ray(origin, direction);
			}

			if (type == typeof(UnityEngine.Plane)) {
				var normal = new UnityEngine.Vector3(
					FromBigEndianSingle(data, 0),
					FromBigEndianSingle(data, 4),
					FromBigEndianSingle(data, 8)
				);
				var distance = FromBigEndianSingle(data, 12);
				return new UnityEngine.Plane(normal, distance);
			}

			// .NET DateTime/TimeSpan
			if (type == typeof(DateTime)) return DateTime.FromBinary(FromBigEndianInt64(data, 0));
			if (type == typeof(TimeSpan)) return new TimeSpan(FromBigEndianInt64(data, 0));
			if (type == typeof(DateTimeOffset)) {
				var dateTime = DateTime.FromBinary(FromBigEndianInt64(data, 0));
				var offset   = new TimeSpan(FromBigEndianInt64(data, 8));
				return new DateTimeOffset(dateTime, offset);
			}

			// .NET Guid
			if (type == typeof(Guid)) return new Guid(data);

			// .NET Numerics types
			if (type == typeof(BigInteger)) return new BigInteger(data);
			if (type == typeof(Complex)) {
				return new Complex(
					FromBigEndianDouble(data, 0),
					FromBigEndianDouble(data, 8)
				);
			}

			// Decimal
			if (type == typeof(decimal)) {
				var bits = new int[4];
				for (int i = 0; i < 4; i++) {
					bits[i] = FromBigEndianInt32(data, i * 4);
				}

				return new decimal(bits);
			}

			// Enum types
			if (type.IsEnum) {
				var underlyingType = Enum.GetUnderlyingType(type);
				var value          = FromBytes(data, underlyingType);
				return Enum.ToObject(type, value);
			}

			// Collections and other complex types
			if (typeof(IDictionary).IsAssignableFrom(type)) {
				return DeserializeDictionary(data, type);
			}

			if (typeof(ICollection).IsAssignableFrom(type)) {
				return DeserializeCollection(data, type);
			}

			if (typeof(IEnumerable).IsAssignableFrom(type)) {
				return DeserializeEnumerable(data, type);
			}

			// Fallback for serializable objects
			if (type.IsSerializable) {
				return DeserializeObject(data, type);
			}

			return GetDefaultValue(type);
		}

		public static T FromBytes<T>(this byte[] data) {
			return (T)FromBytes(data, typeof(T));
		}

		private static object GetDefaultValue(Type type) {
			return type.IsValueType ? Activator.CreateInstance(type) : null;
		}

		private static AnimationCurve DeserializeAnimationCurve(byte[] data) {
			var offset   = 0;
			var keyCount = FromBigEndianInt32(data, offset);
			offset += 4;

			var keys = new Keyframe[keyCount];
			for (int i = 0; i < keyCount; i++) {
				var keyBytes = new byte[16]; // Keyframe is 4 floats
				Array.Copy(data, offset, keyBytes, 0, 16);
				keys[i] =  (Keyframe)FromBytes(keyBytes, typeof(Keyframe));
				offset  += 16;
			}

			var curve = new AnimationCurve(keys);
			return curve;
		}

		private static object DeserializeDictionary(byte[] data, Type dictionaryType) {
			var offset = 0;
			var count  = FromBigEndianInt32(data, offset);
			offset += 4;

			var dictionary  = (IDictionary)Activator.CreateInstance(dictionaryType);
			var genericArgs = dictionaryType.GetGenericArguments();
			var keyType     = genericArgs.Length > 0 ? genericArgs[0] : typeof(object);
			var valueType   = genericArgs.Length > 1 ? genericArgs[1] : typeof(object);

			for (int i = 0; i < count; i++) {
				var keyLength = FromBigEndianInt32(data, offset);
				offset += 4;
				var keyBytes = new byte[keyLength];
				Array.Copy(data, offset, keyBytes, 0, keyLength);
				offset += keyLength;

				var valueLength = FromBigEndianInt32(data, offset);
				offset += 4;
				var valueBytes = new byte[valueLength];
				Array.Copy(data, offset, valueBytes, 0, valueLength);
				offset += valueLength;

				var key   = FromBytes(keyBytes, keyType);
				var value = FromBytes(valueBytes, valueType);
				dictionary.Add(key, value);
			}

			return dictionary;
		}

		private static object DeserializeCollection(byte[] data, Type collectionType) {
			var offset = 0;
			var count  = FromBigEndianInt32(data, offset);
			offset += 4;

			var collection  = (ICollection)Activator.CreateInstance(collectionType);
			var genericArgs = collectionType.GetGenericArguments();
			var itemType    = genericArgs.Length > 0 ? genericArgs[0] : typeof(object);

			var addMethod = collectionType.GetMethod("Add");

			for (int i = 0; i < count; i++) {
				var itemLength = FromBigEndianInt32(data, offset);
				offset += 4;
				var itemBytes = new byte[itemLength];
				Array.Copy(data, offset, itemBytes, 0, itemLength);
				offset += itemLength;

				var item = FromBytes(itemBytes, itemType);
				addMethod?.Invoke(collection, new[] { item });
			}

			return collection;
		}

		private static object DeserializeEnumerable(byte[] data, Type enumerableType) {
			// For most cases, treat as collection
			return DeserializeCollection(data, enumerableType);
		}

		private static object DeserializeObject(byte[] data, Type objectType) {
			try {
				using var stream    = new MemoryStream(data);
				var       formatter = new BinaryFormatter();
				#pragma warning disable SYSLIB0011 // Type or member is obsolete
				return formatter.Deserialize(stream);
				#pragma warning restore SYSLIB0011 // Type or member is obsolete
			} catch {
				// Fallback: try to parse as string if it's a simple type
				var str = Encoding.UTF8.GetString(data);
				return str;
			}
		}

		private static byte[] CombineBytes(params byte[][] arrays) {
			var length = arrays.Sum(array => array.Length);
			var result = new byte[length];
			var offset = 0;

			foreach (var array in arrays) {
				Array.Copy(array, 0, result, offset, array.Length);
				offset += array.Length;
			}

			return result;
		}

		public static int Hash(this string s)
			=> Animator.StringToHash(s);

		public static bool ToBool(this object value)
			=> value switch {
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

		public static byte ToByte(this object value)
			=> value switch {
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

		public static int ToInt(this object value)
			=> value switch {
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
				byte[] b  => b.Length >= 4 ? FromBigEndianInt32(b, 0) : 0,
				string s  => int.TryParse(s, out var result) ? result : 0,
				_         => 0
			};

		public static float ToFloat(this object value)
			=> value switch {
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
				byte[] b  => b.Length >= 4 ? FromBigEndianSingle(b, 0) : 0f,
				string s  => float.TryParse(s, out var result) ? result : 0f,
				_         => 0f
			};

		public static string ToString(this object value)
			=> value switch {
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
				byte[] b  => Encoding.UTF8.GetString(b),
				string s  => s,
				_         => value?.ToString() ?? ""
			};

		public static short ToShort(this object value)
			=> value switch {
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
				byte[] b  => b.Length >= 2 ? FromBigEndianInt16(b, 0) : (short)0,
				string s  => short.TryParse(s, out var result) ? result : (short)0,
				_         => 0
			};

		public static ushort ToUShort(this object value)
			=> value switch {
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
				byte[] b  => b.Length >= 2 ? FromBigEndianUInt16(b, 0) : (ushort)0,
				string s  => ushort.TryParse(s, out var result) ? result : (ushort)0,
				_         => 0
			};

		public static uint ToUInt(this object value)
			=> value switch {
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
				byte[] b  => b.Length >= 4 ? FromBigEndianUInt32(b, 0) : (uint)0,
				string s  => uint.TryParse(s, out var result) ? result : (uint)0,
				_         => 0
			};

		public static long ToLong(this object value)
			=> value switch {
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
				byte[] b  => b.Length >= 8 ? FromBigEndianInt64(b, 0) : 0L,
				string s  => long.TryParse(s, out var result) ? result : 0L,
				_         => 0L
			};

		public static ulong ToULong(this object value)
			=> value switch {
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
				byte[] b  => b.Length >= 8 ? FromBigEndianUInt64(b, 0) : 0UL,
				string s  => ulong.TryParse(s, out var result) ? result : 0UL,
				_         => 0UL
			};

		public static double ToDouble(this object value)
			=> value switch {
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
				byte[] b  => b.Length >= 8 ? FromBigEndianDouble(b, 0) : 0.0,
				string s  => double.TryParse(s, out var result) ? result : 0.0,
				_         => 0.0
			};

		public static Vector3 ToVector3(this object value)
			=> value switch {
				Vector3 v => v,
				byte[] { Length: 12 } b => new Vector3(
					FromBigEndianSingle(b, 0),
					FromBigEndianSingle(b, 4),
					FromBigEndianSingle(b, 8)
				),
				_ => Vector3.zero
			};

		public static Quaternion ToQuaternion(this object value)
			=> value switch {
				Quaternion q => q,
				byte[] { Length: 16 } b => new Quaternion(
					FromBigEndianSingle(b, 0),
					FromBigEndianSingle(b, 4),
					FromBigEndianSingle(b, 8),
					FromBigEndianSingle(b, 12)
				),
				_ => Quaternion.identity
			};
		
		public static byte[] FromVector3(Vector3 v) {
			return CombineBytes(
				ToBigEndian(v.x),
				ToBigEndian(v.y),
				ToBigEndian(v.z)
			);
		}
		

		public static byte[] FromQuaternion(Quaternion q) {
			return CombineBytes(
				ToBigEndian(q.x),
				ToBigEndian(q.y),
				ToBigEndian(q.z),
				ToBigEndian(q.w)
			);
		}
	}
}