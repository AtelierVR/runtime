using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

namespace Nox.CCK.Network {
	public static class Serializer {
		private static byte[] SerializeCollection(ICollection collection) {
			var items = new List<byte[]>();

			// Add count first
			items.Add(BitConverter.GetBytes(collection.Count));

			// Add each item
			foreach (var item in collection) {
				var itemBytes = item.ToBytes();
				items.Add(BitConverter.GetBytes(itemBytes.Length)); // Length prefix
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
			items.Add(BitConverter.GetBytes(count));

			// Add each item with length prefix
			foreach (var item in itemList) {
				var itemBytes = item.ToBytes();
				items.Add(BitConverter.GetBytes(itemBytes.Length));
				items.Add(itemBytes);
			}

			return CombineBytes(items.ToArray());
		}

		private static byte[] SerializeMatrix4X4(UnityEngine.Matrix4x4 matrix) {
			var bytes = new List<byte[]>();
			for (int i = 0; i < 16; i++) {
				bytes.Add(BitConverter.GetBytes(matrix[i]));
			}

			return CombineBytes(bytes.ToArray());
		}

		private static byte[] SerializeAnimationCurve(AnimationCurve curve) {
			var items = new List<byte[]>();
			items.Add(BitConverter.GetBytes(curve.keys.Length));

			foreach (var key in curve.keys) {
				items.Add(key.ToBytes());
			}

			return CombineBytes(items.ToArray());
		}

		private static byte[] SerializeDictionary(IDictionary dictionary) {
			var items = new List<byte[]>();

			// Add count first
			items.Add(BitConverter.GetBytes(dictionary.Count));

			// Add each key-value pair
			foreach (DictionaryEntry entry in dictionary) {
				var keyBytes   = entry.Key.ToBytes();
				var valueBytes = entry.Value.ToBytes();

				items.Add(BitConverter.GetBytes(keyBytes.Length));
				items.Add(keyBytes);
				items.Add(BitConverter.GetBytes(valueBytes.Length));
				items.Add(valueBytes);
			}

			return CombineBytes(items.ToArray());
		}

		private static byte[] SerializeException(Exception ex) {
			var items = new List<byte[]>();
			items.Add(Encoding.UTF8.GetBytes(ex.GetType().Name));
			items.Add(Encoding.UTF8.GetBytes(ex.Message    ?? string.Empty));
			items.Add(Encoding.UTF8.GetBytes(ex.StackTrace ?? string.Empty));

			return CombineBytes(items.ToArray());
		}

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
				short s      => BitConverter.GetBytes(s),
				ushort us    => BitConverter.GetBytes(us),
				int i        => BitConverter.GetBytes(i),
				uint ui      => BitConverter.GetBytes(ui),
				long l       => BitConverter.GetBytes(l),
				ulong ul     => BitConverter.GetBytes(ul),
				float f      => BitConverter.GetBytes(f),
				double d     => BitConverter.GetBytes(d),
				char c       => BitConverter.GetBytes(c),
				string str   => Encoding.UTF8.GetBytes(str),
				byte[] bytes => bytes,
				// Unity Vector types
				UnityEngine.Vector2 v2 => CombineBytes(
					BitConverter.GetBytes(v2.x),
					BitConverter.GetBytes(v2.y)
				),
				UnityEngine.Vector3 v3 => CombineBytes(
					BitConverter.GetBytes(v3.x),
					BitConverter.GetBytes(v3.y),
					BitConverter.GetBytes(v3.z)
				),
				UnityEngine.Vector4 v4 => CombineBytes(
					BitConverter.GetBytes(v4.x),
					BitConverter.GetBytes(v4.y),
					BitConverter.GetBytes(v4.z),
					BitConverter.GetBytes(v4.w)
				),
				UnityEngine.Quaternion q => CombineBytes(
					BitConverter.GetBytes(q.x),
					BitConverter.GetBytes(q.y),
					BitConverter.GetBytes(q.z),
					BitConverter.GetBytes(q.w)
				),
				// Unity Integer Vector types
				Vector2Int v2I => CombineBytes(
					BitConverter.GetBytes(v2I.x),
					BitConverter.GetBytes(v2I.y)
				),
				Vector3Int v3I => CombineBytes(
					BitConverter.GetBytes(v3I.x),
					BitConverter.GetBytes(v3I.y),
					BitConverter.GetBytes(v3I.z)
				),
				// Unity Color types
				Color col => CombineBytes(
					BitConverter.GetBytes(col.r),
					BitConverter.GetBytes(col.g),
					BitConverter.GetBytes(col.b),
					BitConverter.GetBytes(col.a)
				),
				Color32 col32 => new[] { col32.r, col32.g, col32.b, col32.a },
				// Unity Rect/Bounds types
				Rect rect => CombineBytes(
					BitConverter.GetBytes(rect.x),
					BitConverter.GetBytes(rect.y),
					BitConverter.GetBytes(rect.width),
					BitConverter.GetBytes(rect.height)
				),
				RectInt rectInt => CombineBytes(
					BitConverter.GetBytes(rectInt.x),
					BitConverter.GetBytes(rectInt.y),
					BitConverter.GetBytes(rectInt.width),
					BitConverter.GetBytes(rectInt.height)
				),
				Bounds bounds => CombineBytes(
					BitConverter.GetBytes(bounds.center.x),
					BitConverter.GetBytes(bounds.center.y),
					BitConverter.GetBytes(bounds.center.z),
					BitConverter.GetBytes(bounds.size.x),
					BitConverter.GetBytes(bounds.size.y),
					BitConverter.GetBytes(bounds.size.z)
				),
				BoundsInt boundsInt => CombineBytes(
					BitConverter.GetBytes(boundsInt.center.x),
					BitConverter.GetBytes(boundsInt.center.y),
					BitConverter.GetBytes(boundsInt.center.z),
					BitConverter.GetBytes(boundsInt.size.x),
					BitConverter.GetBytes(boundsInt.size.y),
					BitConverter.GetBytes(boundsInt.size.z)
				),
				// Unity Matrix types
				UnityEngine.Matrix4x4 mat => SerializeMatrix4X4(mat),
				// Unity Animation/Keyframe types
				Keyframe kf => CombineBytes(
					BitConverter.GetBytes(kf.time),
					BitConverter.GetBytes(kf.value),
					BitConverter.GetBytes(kf.inTangent),
					BitConverter.GetBytes(kf.outTangent)
				),
				AnimationCurve curve => SerializeAnimationCurve(curve),
				// Unity Physics types
				Ray ray => CombineBytes(
					BitConverter.GetBytes(ray.origin.x),
					BitConverter.GetBytes(ray.origin.y),
					BitConverter.GetBytes(ray.origin.z),
					BitConverter.GetBytes(ray.direction.x),
					BitConverter.GetBytes(ray.direction.y),
					BitConverter.GetBytes(ray.direction.z)
				),
				UnityEngine.Plane plane => CombineBytes(
					BitConverter.GetBytes(plane.normal.x),
					BitConverter.GetBytes(plane.normal.y),
					BitConverter.GetBytes(plane.normal.z),
					BitConverter.GetBytes(plane.distance)
				),
				// .NET DateTime/TimeSpan
				DateTime dt => BitConverter.GetBytes(dt.ToBinary()),
				TimeSpan ts => BitConverter.GetBytes(ts.Ticks),
				DateTimeOffset dto => CombineBytes(
					BitConverter.GetBytes(dto.DateTime.ToBinary()),
					BitConverter.GetBytes(dto.Offset.Ticks)
				),
				// .NET Guid
				Guid guid => guid.ToByteArray(),
				// .NET Numerics types
				BigInteger bigInt => bigInt.ToByteArray(),
				Complex complex => CombineBytes(
					BitConverter.GetBytes(complex.Real),
					BitConverter.GetBytes(complex.Imaginary)
				),
				// Decimal (complex type)
				decimal dec => CombineBytes(
					decimal.GetBits(dec).SelectMany(BitConverter.GetBytes).ToArray()
				),
				// .NET Tuple types (up to 8 items)
				ValueTuple<byte> vt1       => new[] { vt1.Item1 },
				ValueTuple<byte, byte> vt2 => new[] { vt2.Item1, vt2.Item2 },
				ValueTuple<int, int> vt2I => CombineBytes(
					BitConverter.GetBytes(vt2I.Item1),
					BitConverter.GetBytes(vt2I.Item2)
				),
				// Collections
				IEnumerable<byte> byteEnum => byteEnum.ToArray(),
				IDictionary dict           => SerializeDictionary(dict),
				ICollection collection     => SerializeCollection(collection),
				IEnumerable enumerable     => SerializeEnumerable(enumerable),
				// Enum types
				Enum enumValue => BitConverter.GetBytes(Convert.ToInt64(enumValue)),
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
			if (type == typeof(short)) return BitConverter.ToInt16(data, 0);
			if (type == typeof(ushort)) return BitConverter.ToUInt16(data, 0);
			if (type == typeof(int)) return BitConverter.ToInt32(data, 0);
			if (type == typeof(uint)) return BitConverter.ToUInt32(data, 0);
			if (type == typeof(long)) return BitConverter.ToInt64(data, 0);
			if (type == typeof(ulong)) return BitConverter.ToUInt64(data, 0);
			if (type == typeof(float)) return BitConverter.ToSingle(data, 0);
			if (type == typeof(double)) return BitConverter.ToDouble(data, 0);
			if (type == typeof(char)) return BitConverter.ToChar(data, 0);
			if (type == typeof(string)) return Encoding.UTF8.GetString(data);
			if (type == typeof(byte[])) return data;

			// Unity Vector types
			if (type == typeof(UnityEngine.Vector2)) {
				return new UnityEngine.Vector2(
					BitConverter.ToSingle(data, 0),
					BitConverter.ToSingle(data, 4)
				);
			}

			if (type == typeof(UnityEngine.Vector3)) {
				return new UnityEngine.Vector3(
					BitConverter.ToSingle(data, 0),
					BitConverter.ToSingle(data, 4),
					BitConverter.ToSingle(data, 8)
				);
			}

			if (type == typeof(UnityEngine.Vector4)) {
				return new UnityEngine.Vector4(
					BitConverter.ToSingle(data, 0),
					BitConverter.ToSingle(data, 4),
					BitConverter.ToSingle(data, 8),
					BitConverter.ToSingle(data, 12)
				);
			}

			if (type == typeof(UnityEngine.Quaternion)) {
				return new UnityEngine.Quaternion(
					BitConverter.ToSingle(data, 0),
					BitConverter.ToSingle(data, 4),
					BitConverter.ToSingle(data, 8),
					BitConverter.ToSingle(data, 12)
				);
			}

			// Unity Integer Vector types
			if (type == typeof(Vector2Int)) {
				return new Vector2Int(
					BitConverter.ToInt32(data, 0),
					BitConverter.ToInt32(data, 4)
				);
			}

			if (type == typeof(Vector3Int)) {
				return new Vector3Int(
					BitConverter.ToInt32(data, 0),
					BitConverter.ToInt32(data, 4),
					BitConverter.ToInt32(data, 8)
				);
			}

			// Unity Color types
			if (type == typeof(Color)) {
				return new Color(
					BitConverter.ToSingle(data, 0),
					BitConverter.ToSingle(data, 4),
					BitConverter.ToSingle(data, 8),
					BitConverter.ToSingle(data, 12)
				);
			}

			if (type == typeof(Color32)) {
				return new Color32(data[0], data[1], data[2], data[3]);
			}

			// Unity Rect/Bounds types
			if (type == typeof(Rect)) {
				return new Rect(
					BitConverter.ToSingle(data, 0),
					BitConverter.ToSingle(data, 4),
					BitConverter.ToSingle(data, 8),
					BitConverter.ToSingle(data, 12)
				);
			}

			if (type == typeof(RectInt)) {
				return new RectInt(
					BitConverter.ToInt32(data, 0),
					BitConverter.ToInt32(data, 4),
					BitConverter.ToInt32(data, 8),
					BitConverter.ToInt32(data, 12)
				);
			}

			if (type == typeof(Bounds)) {
				var center = new UnityEngine.Vector3(
					BitConverter.ToSingle(data, 0),
					BitConverter.ToSingle(data, 4),
					BitConverter.ToSingle(data, 8)
				);
				var size = new UnityEngine.Vector3(
					BitConverter.ToSingle(data, 12),
					BitConverter.ToSingle(data, 16),
					BitConverter.ToSingle(data, 20)
				);
				return new Bounds(center, size);
			}

			if (type == typeof(BoundsInt)) {
				var center = new Vector3Int(
					BitConverter.ToInt32(data, 0),
					BitConverter.ToInt32(data, 4),
					BitConverter.ToInt32(data, 8)
				);
				var size = new Vector3Int(
					BitConverter.ToInt32(data, 12),
					BitConverter.ToInt32(data, 16),
					BitConverter.ToInt32(data, 20)
				);
				return new BoundsInt(center, size);
			}

			// Unity Matrix types
			if (type == typeof(UnityEngine.Matrix4x4)) {
				var matrix = new UnityEngine.Matrix4x4();
				for (int i = 0; i < 16; i++) {
					matrix[i] = BitConverter.ToSingle(data, i * 4);
				}

				return matrix;
			}

			// Unity Animation/Keyframe types
			if (type == typeof(Keyframe)) {
				return new Keyframe(
					BitConverter.ToSingle(data, 0),
					BitConverter.ToSingle(data, 4),
					BitConverter.ToSingle(data, 8),
					BitConverter.ToSingle(data, 12)
				);
			}

			if (type == typeof(AnimationCurve)) {
				return DeserializeAnimationCurve(data);
			}

			// Unity Physics types
			if (type == typeof(Ray)) {
				var origin = new UnityEngine.Vector3(
					BitConverter.ToSingle(data, 0),
					BitConverter.ToSingle(data, 4),
					BitConverter.ToSingle(data, 8)
				);
				var direction = new UnityEngine.Vector3(
					BitConverter.ToSingle(data, 12),
					BitConverter.ToSingle(data, 16),
					BitConverter.ToSingle(data, 20)
				);
				return new Ray(origin, direction);
			}

			if (type == typeof(UnityEngine.Plane)) {
				var normal = new UnityEngine.Vector3(
					BitConverter.ToSingle(data, 0),
					BitConverter.ToSingle(data, 4),
					BitConverter.ToSingle(data, 8)
				);
				var distance = BitConverter.ToSingle(data, 12);
				return new UnityEngine.Plane(normal, distance);
			}

			// .NET DateTime/TimeSpan
			if (type == typeof(DateTime)) return DateTime.FromBinary(BitConverter.ToInt64(data, 0));
			if (type == typeof(TimeSpan)) return new TimeSpan(BitConverter.ToInt64(data, 0));
			if (type == typeof(DateTimeOffset)) {
				var dateTime = DateTime.FromBinary(BitConverter.ToInt64(data, 0));
				var offset   = new TimeSpan(BitConverter.ToInt64(data, 8));
				return new DateTimeOffset(dateTime, offset);
			}

			// .NET Guid
			if (type == typeof(Guid)) return new Guid(data);

			// .NET Numerics types
			if (type == typeof(BigInteger)) return new BigInteger(data);
			if (type == typeof(Complex)) {
				return new Complex(
					BitConverter.ToDouble(data, 0),
					BitConverter.ToDouble(data, 8)
				);
			}

			// Decimal
			if (type == typeof(decimal)) {
				var bits = new int[4];
				for (int i = 0; i < 4; i++) {
					bits[i] = BitConverter.ToInt32(data, i * 4);
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
			var keyCount = BitConverter.ToInt32(data, offset);
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
			var count  = BitConverter.ToInt32(data, offset);
			offset += 4;

			var dictionary  = (IDictionary)Activator.CreateInstance(dictionaryType);
			var genericArgs = dictionaryType.GetGenericArguments();
			var keyType     = genericArgs.Length > 0 ? genericArgs[0] : typeof(object);
			var valueType   = genericArgs.Length > 1 ? genericArgs[1] : typeof(object);

			for (int i = 0; i < count; i++) {
				var keyLength = BitConverter.ToInt32(data, offset);
				offset += 4;
				var keyBytes = new byte[keyLength];
				Array.Copy(data, offset, keyBytes, 0, keyLength);
				offset += keyLength;

				var valueLength = BitConverter.ToInt32(data, offset);
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
			var count  = BitConverter.ToInt32(data, offset);
			offset += 4;

			var collection  = (ICollection)Activator.CreateInstance(collectionType);
			var genericArgs = collectionType.GetGenericArguments();
			var itemType    = genericArgs.Length > 0 ? genericArgs[0] : typeof(object);

			var addMethod = collectionType.GetMethod("Add");

			for (int i = 0; i < count; i++) {
				var itemLength = BitConverter.ToInt32(data, offset);
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
	}
}