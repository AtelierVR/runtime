using FFmpeg.AutoGen;

namespace Hactazia.FFPlay.Core {
	public enum MediaType {
		Video      = AVMediaType.AVMEDIA_TYPE_VIDEO,
		Audio      = AVMediaType.AVMEDIA_TYPE_AUDIO,
		Subtitle   = AVMediaType.AVMEDIA_TYPE_SUBTITLE,
		Data       = AVMediaType.AVMEDIA_TYPE_DATA,
		Attachment = AVMediaType.AVMEDIA_TYPE_ATTACHMENT,
	}
}