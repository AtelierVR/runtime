namespace Nox.Instances {
	public interface IInstance {
		public uint        GetId();
		public string      GetServerAddress();
		public string      GetName();
		public string      GetTitle();
		public string      GetDescription();
		public string      GetThumbnailUrl();
		public IOwner      GetOwner();
		public string[]    GetTags();
		public string      GetWorldId();
		public IConnection GetConnectionData();
		public ushort      GetPlayerCount();
		public IPlayer[]   GetPlayers();
		public ushort      GetCapacity();

		public IInstanceIdentifier ToIdentifier();
	}
}