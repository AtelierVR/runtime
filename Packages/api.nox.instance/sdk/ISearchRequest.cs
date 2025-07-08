using Nox.Users;
using Nox.Worlds;

namespace Nox.Instances {
	public interface ISearchRequest {
		public string GetQuery();
		
		public IUserIdentifier GetOwner();

		public IWorldIdentifier GetWorld();

		public uint GetOffset();

		public uint GetLimit();

		public ISearchRequest SetQuery(string query);
		
		public ISearchRequest SetOwner(IUserIdentifier owner);

		public ISearchRequest SetWorld(IWorldIdentifier world);

		public ISearchRequest SetOffset(uint offset);

		public ISearchRequest SetLimit(uint limit);
	}
}