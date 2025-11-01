using System;
using Nox.Avatars.Parameters;
using Nox.CCK.Network;
using UnityEngine;

namespace Nox.CCK.Avatars.Parameters {
	public abstract class BaseParameter : IParameter {
		internal AnimatorControllerParameter Parameter;
		internal ParameterEntry              Entry;

		public string GetName()
			=> Parameter.name;

		public int GetHash()
			=> Parameter.nameHash;

		public bool IsSyncable()
			=> Entry?.synced ?? false;

		public bool IsSavable()
			=> Entry?.savable ?? false;

		public bool IsReadOnly()
			=> false;

		public ParameterType GetValueType()
			=> Parameter.type switch {
				AnimatorControllerParameterType.Float => ParameterType.Float,
				AnimatorControllerParameterType.Int   => ParameterType.Int,
				AnimatorControllerParameterType.Bool  => ParameterType.Bool,
				_                                     => ParameterType.Float
			};

		public object Get()
			=> Parameter.type switch {
				AnimatorControllerParameterType.Float   => GetFloat(),
				AnimatorControllerParameterType.Int     => GetInteger(),
				AnimatorControllerParameterType.Bool    => GetBool(),
				AnimatorControllerParameterType.Trigger => throw new InvalidOperationException("Cannot get value of a Trigger parameter."),
				_                                       => throw new ArgumentOutOfRangeException()
			};

		public void Set(object value) {
			switch (GetValueType()) {
				case ParameterType.Float:
				case ParameterType.Double:
					SetFloat(value.ToFloat());
					break;
				case ParameterType.Byte:
				case ParameterType.Short:
				case ParameterType.UShort:
				case ParameterType.UInt:
				case ParameterType.Long:
				case ParameterType.ULong:
				case ParameterType.Int:
					SetInteger(value.ToInt());
					break;
				case ParameterType.Bool:
					SetBool(value.ToBool());
					break;
				case ParameterType.ByteArray:
				case ParameterType.String:
				case ParameterType.Vector3:
				case ParameterType.Quaternion:
				default:
					throw new ArgumentOutOfRangeException($"Unsupported parameter type: {Parameter.type}");
			}
		}

		public byte[] Serialize()
			=> Get().ToBytes();

		public void Deserialize(byte[] data)
			=> Set(data);

		protected abstract void SetFloat(float value);

		protected abstract void SetInteger(int value);

		protected abstract void SetBool(bool value);

		protected abstract float GetFloat();

		protected abstract int GetInteger();

		protected abstract bool GetBool();
	}
}