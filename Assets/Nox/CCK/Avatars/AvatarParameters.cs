using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nox.CCK.Avatars
{
    [CreateAssetMenu(fileName = "AvatarParameters", menuName = "Nox/Avatars/Avatar Parameters")]
    public class AvatarParameters : ScriptableObject
    {
        public Parameter[] parameters;

        public Parameter GetParameter(string key)
            => parameters.FirstOrDefault(parameter => parameter.key == key);

        public Parameter GetParameter(uint key)
            => parameters.Length > key ? parameters[key] : null;

        [Serializable]
        public class Parameter
        {
            public string key;
            public byte[] value;
            public ParameterType type;
            public ParameterFlags flags;

            public bool AsBool
            {
                get => BitConverter.ToBoolean(value, 0);
                set
                {
                    this.value = BitConverter.GetBytes(value);
                    type = ParameterType.Bool;
                }
            }

            public string AsString
            {
                get => BitConverter.ToString(value);
                set
                {
                    this.value = Encoding.UTF8.GetBytes(value);
                    type = ParameterType.String;
                }
            }

            public sbyte AsInt8
            {
                get => (sbyte)value[0];
                set
                {
                    this.value = new[] { (byte)value };
                    type = ParameterType.Int8;
                }
            }

            public byte AsUInt8
            {
                get => value[0];
                set
                {
                    this.value = new[] { value };
                    type = ParameterType.UInt8;
                }
            }

            public short AsInt16
            {
                get => BitConverter.ToInt16(value, 0);
                set
                {
                    this.value = BitConverter.GetBytes(value);
                    type = ParameterType.Int16;
                }
            }

            public ushort AsUInt16
            {
                get => BitConverter.ToUInt16(value, 0);
                set
                {
                    this.value = BitConverter.GetBytes(value);
                    type = ParameterType.UInt16;
                }
            }

            public int AsInt32
            {
                get => BitConverter.ToInt32(value, 0);
                set
                {
                    this.value = BitConverter.GetBytes(value);
                    type = ParameterType.Int32;
                }
            }

            public uint AsUInt32
            {
                get => BitConverter.ToUInt32(value, 0);
                set
                {
                    this.value = BitConverter.GetBytes(value);
                    type = ParameterType.UInt32;
                }
            }

            public float AsFloat8
            {
                get => value[0] / 255f;
                set
                {
                    this.value = new[] { (byte)(value * 255) };
                    type = ParameterType.Float8;
                }
            }
            
            public float AsFloat16
            {
                get => BitConverter.ToUInt16(value, 0) / 65535f;
                set
                {
                    this.value = BitConverter.GetBytes((ushort)(value * 65535));
                    type = ParameterType.Float16;
                }
            }
            
            public float AsFloat32
            {
                get => BitConverter.ToUInt32(value, 0) / 4294967295f;
                set
                {
                    this.value = BitConverter.GetBytes((uint)(value * 4294967295));
                    type = ParameterType.Float32;
                }
            }
            
            public T As<T>() where T : struct
                => type switch
                {
                    ParameterType.Bool => (T)(object)AsBool,
                    ParameterType.String => (T)(object)AsString,
                    ParameterType.Int8 => (T)(object)AsInt8,
                    ParameterType.UInt8 => (T)(object)AsUInt8,
                    ParameterType.Int16 => (T)(object)AsInt16,
                    ParameterType.UInt16 => (T)(object)AsUInt16,
                    ParameterType.Int32 => (T)(object)AsInt32,
                    ParameterType.UInt32 => (T)(object)AsUInt32,
                    ParameterType.Float8 => (T)(object)AsFloat8,
                    ParameterType.Float16 => (T)(object)AsFloat16,
                    ParameterType.Float32 => (T)(object)AsFloat32,
                    _ => default
                };
        }
    }

    [Flags]
    public enum ParameterFlags
    {
        None = 0,
        Saved = 1,
        NetworkSynced = 2,
    }

    public enum ParameterType : byte
    {
        Bool = 1,

        String = 2,

        Int8 = 3,
        UInt8 = 4,
        Int16 = 5,
        UInt16 = 6,
        Int32 = 7,
        UInt32 = 8,

        Float8 = 9,
        Float16 = 10,
        Float32 = 11,
    }
}