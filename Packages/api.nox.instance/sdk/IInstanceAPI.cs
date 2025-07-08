using Cysharp.Threading.Tasks;

namespace Nox.Instances {
	public interface IInstanceAPI {
		public UniTask<IInstance> Fetch(IInstanceIdentifier identifier);

		public UniTask<IInstance> Fetch(uint id, string from = null);

		public UniTask<IInstance> Fetch(string identifier, string from = null);

		public UniTask<ISearchResponse> Search(ISearchRequest data, string from = null);

		public ISearchRequest MakeSearchRequest();

		public IInstanceIdentifier Make(string identifier);
	}
}