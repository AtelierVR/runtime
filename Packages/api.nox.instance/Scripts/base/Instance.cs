using System;
using System.Linq;
using Newtonsoft.Json;
using Nox.CCK.Convertors;
using Nox.CCK.Utils;
using Nox.Instances;

// ReSharper disable once InconsistentNaming
namespace api.nox.instance {
	[Serializable]
	public class Instance : IInstance, INoxObject {
		public uint             id;
		public string           server;
		public string           name;
		public string           title;
		public string           description;
		public string           thumbnail;
		public ushort           capacity;
		public string           owner;
		public string[]         tags;
		public Connection       connection;
		public ushort           client_count;
		public InstancePlayer[] players;

		public uint GetId()
			=> id;

		public string GetServer()
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

		[JsonProperty("world"), JsonConverter(typeof(StringToIdentifierConverter))]
		public Identifier World { get; private set; }

		public IConnection GetConnectionData()
			=> connection;

		public ushort GetPlayerCount()
			=> client_count;

		public ushort GetCapacity()
			=> capacity;

		public IInstancePlayer[] GetPlayers()
			=> players.Cast<IInstancePlayer>().ToArray();

		public IInstanceIdentifier ToIdentifier()
			=> new InstanceIdentifier(id, null, server);
	}
}