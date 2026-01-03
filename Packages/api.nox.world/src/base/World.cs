using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using Nox.Worlds;

namespace api.nox.world {
	[Serializable]
	public class World : IWorld, INoxObject {
		public uint     id;
		public string   title;
		public string   description;
		public ushort   capacity;
		public string[] tags;
		public string   owner;
		public string   server;
		public string   thumbnail;
		public string[] contributors;

		public uint GetId()
			=> id;

		public string GetTitle()
			=> title;

		public string GetDescription()
			=> description;

		public ushort GetCapacity()
			=> capacity;

		public string[] GetTags()
			=> tags ?? Array.Empty<string>();

		public string GetServerAddress()
			=> server;

		public string GetOwnerId()
			=> owner;

		public string[] GetContributorIds()
			=> contributors ?? Array.Empty<string>();

		public string GetThumbnailUrl()
			=> thumbnail;

		public async UniTask<IWorld> Refresh()
			=> await InternalRefresh();

		public IWorldIdentifier ToIdentifier()
			=> ToInternalIdentifier();

		public async UniTask<World> InternalRefresh()
			=> await Main.Instance.Network.Fetch(ToInternalIdentifier(), server);

		public WorldIdentifier ToInternalIdentifier()
			=> new(id, null, server);
	}
}