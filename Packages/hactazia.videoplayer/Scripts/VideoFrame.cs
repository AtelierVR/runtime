using System;

namespace Hactazia.VideoPlayer {
	[Serializable]
	public struct VideoFrame {
		public IntPtr data;
		public int    width;
		public int    height;
		public double timestamp;
		public bool   valid;
	}
}