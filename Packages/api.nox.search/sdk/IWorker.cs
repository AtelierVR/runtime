using Cysharp.Threading.Tasks;

namespace Nox.Search {
	public interface IWorker {
		public string GetTitleKey();

		public string[] GetTitleArguments();

		public UniTask<IResult> Fetch(IFetchOptions options);

		public float GetRatio()
			=> 1f;
	}
}