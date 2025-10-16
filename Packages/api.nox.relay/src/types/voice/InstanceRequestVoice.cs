using System;
using api.nox.relay.types.Instance;
using UnityEngine;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Voice {
	public class InstanceRequestVoice : RelayInstanceRequest {
		public short[] Samples = Array.Empty<short>();

		public bool IsEmpty()
			=> Samples.Length == 0;

		public static InstanceRequestVoice CreateRequest(AudioClip clip) {
			var samples  = clip.samples * clip.channels;
			var instance = new InstanceRequestVoice { Samples = new short[samples] };

			var data = new float[samples];
			clip.GetData(data, 0);

			const float multiplier  = short.MaxValue;
			var         samplesSpan = instance.Samples.AsSpan();
			var         dataSpan    = data.AsSpan();

			for (var i = 0; i < samples; i++)
				samplesSpan[i] = (short)(dataSpan[i] * multiplier);

			return instance;
		}

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(InternalId);
			var buff = new byte[Samples.Length * sizeof(short)];
			System.Buffer.BlockCopy(Samples, 0, buff, 0, buff.Length);
			buffer.Write(buff);
			return buffer;
		}
	}
}