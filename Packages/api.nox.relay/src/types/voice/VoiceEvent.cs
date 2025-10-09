using api.nox.relay.types.Instance;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Voice {
	public class VoiceEvent : RelayInstanceResponse {
		public ushort  PlayerId;
		public short[] Samples;

		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);

			InternalId = buffer.ReadByte();
			PlayerId   = buffer.ReadUShort();

			var remaining = buffer.Remaining;
			
			if (remaining == 0)
				return true;
			
			if (remaining % 2 != 0)
				return false;
			
			var sample = buffer.ReadBytes((ushort)remaining);
			Samples = new short[sample.Length / 2];
			for (var i = 0; i < Samples.Length; i++)
				Samples[i] = (short)(sample[i * 2] | (sample[i * 2 + 1] << 8));

			return true;
		}
	}
}