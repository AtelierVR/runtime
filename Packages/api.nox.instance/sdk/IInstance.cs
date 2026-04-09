using Nox.CCK.Utils;
namespace Nox.Instances {
	public interface IInstance {
		public uint GetId();
		public string GetServer();
		public string GetName();
		public string GetTitle();
		public string GetDescription();
		public string GetThumbnailUrl();
		public IOwner GetOwner();
		public string[] GetTags();
		public Identifier World { get; }
		public IConnection GetConnectionData();
		public ushort GetPlayerCount();
		public IInstancePlayer[] GetPlayers();
		public ushort GetCapacity();

		public IInstanceIdentifier ToIdentifier();
	}
}