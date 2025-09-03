namespace Nox.VideoPlayer {
	public interface IResultData {
		public string GetId();

		public string GetTile();

		public string GetSubtitle();

		public string GetDescription();

		public IThumbnail[] GetThumbnails();

		public IFormat[] GetFormat();

		public ISubtitle[] GetSubtitles();
	}
}