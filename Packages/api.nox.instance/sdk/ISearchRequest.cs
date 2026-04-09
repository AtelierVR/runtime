using Nox.CCK.Utils;

namespace Nox.Instances {
	public interface ISearchRequest {
		public string GetQuery();

		public Identifier GetOwner();

		public Identifier GetWorld();

		public uint GetOffset();

		public uint GetLimit();

		public ISearchRequest SetQuery(string query);

		public ISearchRequest SetOwner(Identifier owner);

		public ISearchRequest SetWorld(Identifier world);

		public ISearchRequest SetOffset(uint offset);

		public ISearchRequest SetLimit(uint limit);
	}
}