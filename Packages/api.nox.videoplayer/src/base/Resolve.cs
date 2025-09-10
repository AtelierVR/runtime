using System.Linq;
using Nox.VideoPlayer;

namespace api.nox.videoplayer {
	public class Resolve : IResolve {
		public string      Id;
		public string      Title;
		public string      Subtitle;
		public string      Description;
		public Thumbnail[] Thumbnails;
		public Format[]    Formats;
		public Subtitle[]  Subtitles;

		public string GetId()
			=> Id;

		public string GetTile()
			=> Title;

		public string GetSubtitle()
			=> Subtitle;

		public string GetDescription()
			=> Description;

		public IThumbnail[] GetThumbnails()
			=> Thumbnails.Cast<IThumbnail>().ToArray();

		public IFormat[] GetFormat()
			=> Formats.Cast<IFormat>().ToArray();

		public ISubtitle[] GetSubtitles()
			=> Subtitles.Cast<ISubtitle>().ToArray();
	}
}