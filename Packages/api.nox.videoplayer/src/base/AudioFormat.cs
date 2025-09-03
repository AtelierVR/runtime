using Nox.VideoPlayer;

namespace api.nox.videoplayer {
	public class AudioFormat: Format, IAudio {
		public uint   GetAudioChannels() {
			throw new System.NotImplementedException();
		}
		public uint   GetAudioBitrate() {
			throw new System.NotImplementedException();
		}
		public string GetAudioCodec() {
			throw new System.NotImplementedException();
		}
	}
}