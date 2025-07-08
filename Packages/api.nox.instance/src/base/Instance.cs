using System;
using Nox.CCK.Utils;
using Nox.Instances;

namespace api.nox.instance {
	[Serializable]
	public class Instance : IInstance, INoxObject {
		public uint      id;
		public string    server;
		public string    name;
		public string    title;
		public string    description;
		public string    thumbnail;
		public ushort    capacity;
		public string    owner;
		public string[]  tags;
		public string    world;
		public string    address;
		public ushort    client_count;
		public IPlayer[] players;


		public uint GetId()
			=> id;

		public string GetServerAddress()
			=> server;

		public string GetName()
			=> name;

		public string GetTitle()
			=> title;

		public string GetDescription()
			=> description;

		public string GetThumbnailUrl()
			=> thumbnail;

		public IOwner GetOwner()
			=> new Owner(owner);

		public string[] GetTags()
			=> tags;

		public string GetWorldId()
			=> world;

		public IConnection GetConnectionData()
			=> new Connection(address);

		public ushort GetPlayerCount()
			=> client_count;

		public IPlayer[] GetPlayers()
			=> players;

		public IInstanceIdentifier ToIdentifier()
			=> new InstanceIdentifier(id, null, server);
	}
}