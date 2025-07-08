using Cysharp.Threading.Tasks;

namespace Nox.Users {
	public interface ISearchResponse {
		public string   GetQuery();
		public uint[] GetIds();

		public IUser[] GetUsers();
		public uint    GetTotal();
		public uint    GetLimit();
		public uint    GetOffset();
		public bool    HasNext();
		public bool    HasPrevious();

		public UniTask<ISearchResponse> Next();
		public UniTask<ISearchResponse> Previous();
	}
}