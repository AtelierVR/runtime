using Cysharp.Threading.Tasks;
using Nox.Search;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.user.search {
	public class SearchData : IResultData {
		public User Reference;

		public int GetId()
			=> Reference.ToIdentifier().ToString().GetHashCode();

		public string GetTitleKey()
			=> "user.search.data.title";

		public string[] GetTitleArguments()
			=> new[] { Reference.GetDisplay() ?? Reference.GetUsername() };

		public UniTask<Texture2D> GetImage()
			=> Reference.GetThumbnail();

		public void OnClick() {
			Logger.LogDebug($"SearchData.OnClick: {Reference.GetDisplay()} ({Reference.ToIdentifier()})");
		}
	}
}