using System.Collections.Generic;
using api.nox.relay.types.Instance;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Avatar {
	public class AvatarParamsEvent : RelayInstanceResponse
	{
		public          ushort                  PlayerId;
		public readonly Dictionary<int, byte[]> Parameters = new();

		public override bool FromBuffer(Buffer buffer)
		{
			buffer.Goto(0);
			InternalId = buffer.ReadByte();
			PlayerId = buffer.ReadUShort();
			var parameterCount = buffer.ReadByte();
			for (var i = 0; i < parameterCount; i++)
			{
				var parameterId = buffer.ReadInt();
				var payloadSize = buffer.ReadUShort();
				var payload = buffer.ReadBytes(payloadSize);
				Parameters[parameterId] = payload;
			}
			return true;
		}
	}
}
