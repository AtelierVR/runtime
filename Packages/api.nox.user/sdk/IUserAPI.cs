using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace Nox.Users {
	public interface IUserAPI {
		public ICurrentUser Current { get; }

		public UniTask<ICurrentUser> FetchCurrent();

		public UniTask<IUser> Fetch(Identifier identifier);

		public ISearchRequest MakeSearchRequest();

		public UniTask<ISearchResponse> Search(ISearchRequest request, string from = null);

		public UniTask<IAuthToken> GetToken(string address);

		public UniTask<ICurrentUser> UpdateCurrent(IUpdateCurrentUserRequest request);

		public IUpdateCurrentUserRequest MakeUpdateCurrentRequest();
	}
}