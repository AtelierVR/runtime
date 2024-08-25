﻿using System;
using UnityEngine;

namespace api.nox.network.Utils
{
    public class Buffer
    {
        public byte[] data;
        public ushort offset;
        public ushort length;

        public int Remaining => length - offset;

        public Buffer(ushort offset = 0)
        {
            Clear();
            this.offset = offset;
            length = offset;
        }

        public bool Write(byte value)
        {
            if (offset + 1 > data.Length) return false;
            data[offset++] = value;
            if (offset > length) length = offset;
            return true;
        }

        public bool Write(short value)
        {
            if (offset + 2 > data.Length) return false;
            data[offset++] = (byte)(value >> 8);
            data[offset++] = (byte)value;
            if (offset > length) length = offset;
            return true;
        }

        public bool Write(ushort value) => Write((short)value);

        public bool Write(string value)
        {
            if (offset + value.Length + 2 > data.Length) return false;
            if (!Write((ushort)value.Length)) return false;
            foreach (var c in value)
                data[offset++] = (byte)c;
            if (offset > length) length = offset;
            return true;
        }

        public bool Write(DateTimeOffset value) => Write(value.ToUnixTimeMilliseconds());

        public bool Write(int value)
        {
            if (offset + 4 > data.Length) return false;
            data[offset++] = (byte)(value >> 24);
            data[offset++] = (byte)(value >> 16);
            data[offset++] = (byte)(value >> 8);
            data[offset++] = (byte)value;
            if (offset > length) length = offset;
            return true;
        }

        public bool Write(uint value) => Write((int)value);

        public bool Write(long value)
        {
            if (offset + 8 > data.Length) return false;
            data[offset++] = (byte)(value >> 56);
            data[offset++] = (byte)(value >> 48);
            data[offset++] = (byte)(value >> 40);
            data[offset++] = (byte)(value >> 32);
            data[offset++] = (byte)(value >> 24);
            data[offset++] = (byte)(value >> 16);
            data[offset++] = (byte)(value >> 8);
            data[offset++] = (byte)value;
            if (offset > length) length = offset;
            return true;
        }

        public bool Write(ulong value) => Write((long)value);

        public bool Write(float value)
        {
            if (offset + 4 > data.Length) return false;
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            foreach (var b in bytes)
                data[offset++] = b;
            if (offset > length) length = offset;
            return true;
        }


        public bool Write(double value)
        {
            if (offset + 8 > data.Length) return false;
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            foreach (var b in bytes)
                data[offset++] = b;
            if (offset > length) length = offset;
            return true;
        }

        public bool Write(Vector3 value)
        {
            if (offset + 12 > data.Length) return false;
            if (!Write(value.x)) return false;
            if (!Write(value.y)) return false;
            if (!Write(value.z)) return false;
            if (offset > length) length = offset;
            return true;
        }

        public bool Write(Quaternion value)
        {
            if (offset + 16 > data.Length) return false;
            if (!Write(value.x)) return false;
            if (!Write(value.y)) return false;
            if (!Write(value.z)) return false;
            if (!Write(value.w)) return false;
            if (offset > length) length = offset;
            return true;
        }

        public bool Write(byte[] value)
        {
            if (offset + value.Length > data.Length) return false;
            foreach (var b in value)
                data[offset++] = b;
            if (offset > length) length = offset;
            return true;
        }

        public byte[] ToBuffer()
        {
            var buffer = new byte[length];
            Array.Copy(data, buffer, length);
            return buffer;
        }

        public void Goto(ushort offset) => this.offset = offset;
        public void GotoEnd() => offset = length;
        public void Skip(ushort offset) => this.offset += offset;

        public override string ToString()
        {
            var res = "Buffer[(offset=" + offset + ", length=" + length + ") ";
            for (var i = 0; i < length; i++) res += (i == 0 ? "" : " ") + data[i].ToString("X2");
            return res + "]";
        }

        public byte ReadByte()
        {
            if (offset + 1 > length) return 0;
            return data[offset++];
        }

        public ushort ReadUShort()
        {
            if (offset + 2 > length) return 0;
            var value = (ushort)(data[offset++] << 8);
            value |= data[offset++];
            return value;
        }

        public string ReadString()
        {
            var length = ReadUShort();
            if (offset + length > this.length) return null;
            var value = System.Text.Encoding.UTF8.GetString(data, offset, length);
            offset += length;
            return value;
        }

        public DateTime ReadDateTime()
        {
            var value = ReadLong();
            return DateTimeOffset.FromUnixTimeMilliseconds(value).DateTime;
        }

        public int ReadInt()
        {
            if (offset + 4 > length) return 0;
            var value = data[offset++] << 24;
            value |= data[offset++] << 16;
            value |= data[offset++] << 8;
            value |= data[offset++];
            return value;
        }

        public long ReadLong()
        {
            if (offset + 8 > length) return 0;
            var value = (long)data[offset++] << 56;
            value |= (long)data[offset++] << 48;
            value |= (long)data[offset++] << 40;
            value |= (long)data[offset++] << 32;
            value |= (long)data[offset++] << 24;
            value |= (long)data[offset++] << 16;
            value |= (long)data[offset++] << 8;
            value |= data[offset++];
            return value;
        }

        public float ReadFloat()
        {
            if (offset + 4 > length) return 0;
            var bytes = new byte[4];
            for (var i = 0; i < 4; i++)
                bytes[i] = data[offset++];
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        public double ReadDouble()
        {
            if (offset + 8 > length) return 0;
            var bytes = new byte[8];
            for (var i = 0; i < 8; i++)
                bytes[i] = data[offset++];
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }

        public Vector3 ReadVector3()
        {
            if (offset + 12 > length) return Vector3.zero;
            var x = ReadFloat();
            var y = ReadFloat();
            var z = ReadFloat();
            return new Vector3(x, y, z);
        }

        public Quaternion ReadQuaternion()
        {
            if (offset + 16 > length) return Quaternion.identity;
            var x = ReadFloat();
            var y = ReadFloat();
            var z = ReadFloat();
            var w = ReadFloat();
            return new Quaternion(x, y, z, w);
        }

        public byte[] ReadBytes(ushort length)
        {
            if (offset + length > this.length) return Array.Empty<byte>();
            var value = new byte[length];
            for (var i = 0; i < length; i++)
                value[i] = data[offset++];
            return value;
        }

        public uint ReadUInt() => (uint)ReadInt();

        public Buffer Clone(ushort start = 0, ushort end = 0)
        {
            if (end == 0) end = length;
            var buffer = new Buffer();
            for (var i = start; i < end; i++)
                buffer.Write(data[i]);
            buffer.Goto(0);
            return buffer;
        }

        public void Clear()
        {
            offset = 0;
            length = 0;
            data = new byte[1024];
        }

        public bool Write(Buffer buffer)
        {
            if (offset + buffer.length > data.Length) return false;
            for (var i = 0; i < buffer.length; i++)
                data[offset++] = buffer.data[i];
            if (offset > length) length = offset;
            return true;
        }

        public T Read<T>()
        {
            if (typeof(T) == typeof(byte)) return (T)(object)ReadByte();
            if (typeof(T) == typeof(ushort)) return (T)(object)ReadUShort();
            if (typeof(T) == typeof(string)) return (T)(object)ReadString();
            if (typeof(T) == typeof(DateTime)) return (T)(object)ReadDateTime();
            if (typeof(T) == typeof(int)) return (T)(object)ReadInt();
            if (typeof(T) == typeof(long)) return (T)(object)ReadLong();
            if (typeof(T) == typeof(float)) return (T)(object)ReadFloat();
            if (typeof(T) == typeof(double)) return (T)(object)ReadDouble();
            if (typeof(T) == typeof(Vector3)) return (T)(object)ReadVector3();
            if (typeof(T) == typeof(Quaternion)) return (T)(object)ReadQuaternion();
            if (typeof(T) == typeof(byte[])) return (T)(object)ReadBytes(ReadUShort());
            if (typeof(T) == typeof(uint)) return (T)(object)ReadUInt();
            if (typeof(T) == typeof(Buffer)) return (T)(object)Clone();
            return default;
        }

        public T ReadEnum<T>() where T : Enum
        {
            var type = Enum.GetUnderlyingType(typeof(T));
            if (type == typeof(byte)) return (T)(object)ReadByte();
            if (type == typeof(ushort)) return (T)(object)ReadUShort();
            if (type == typeof(uint)) return (T)(object)ReadUInt();
            return default;
        }

        public bool Write(Enum value)
        {
            var type = Enum.GetUnderlyingType(value.GetType());
            if (type == typeof(byte)) return Write(Convert.ToByte(value));
            if (type == typeof(ushort)) return Write(Convert.ToUInt16(value));
            if (type == typeof(uint)) return Write(Convert.ToUInt32(value));
            return false;
        }
    }
}