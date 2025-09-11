using System;

namespace Hactazia.VideoPlayer {
	[Serializable]
	public struct AudioFrame {
		public IntPtr data;
		public int    size;
		public int    sampleRate;
		public int    channels;
		public double timestamp;
		public bool   valid;
	}
}