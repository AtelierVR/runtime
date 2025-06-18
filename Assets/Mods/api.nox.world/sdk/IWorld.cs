using Cysharp.Threading.Tasks;

namespace Nox.Worlds {
	public interface IWorld {
		public uint GetId();

		public string GetTitle();

		public string GetDescription();

		public ushort GetCapacity();

		public string[] GetTags();

		public string GetServerAddress();
		
		public string GetOwnerId();
		
		public string[] GetContributorIds();
		
		public string GetThumbnailUrl();
		
		public UniTask<IWorld> Refresh();
		
		public IWorldIdentifier ToIdentifier();
	}
}