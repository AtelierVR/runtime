using Cysharp.Threading.Tasks;

namespace Nox.Instances {
	public interface ISearchResponse {
		public string GetQuery();

		public string GetOwnerId();

		public string GetWorldId();

		public IInstance[] GetInstances();

		public uint GetTotal();

		public uint GetLimit();

		public uint GetOffset();

		public bool HasNext();

		public bool HasPrevious();

		public UniTask<ISearchResponse> Next();

		public UniTask<ISearchResponse> Previous();
	}
}