using Cysharp.Threading.Tasks;
using Nox.Search;
using UnityEngine;
using Avatar = api.nox.avatar.network.Avatar;

namespace api.nox.avatar.search {
	public class SearchData : IResultData {
		public Avatar Reference;

		public int GetId()
			=> Reference.GetIdentifier().ToString().GetHashCode();

		public string GetTitleKey()
			=> "avatar.search.data.title";

		public string[] GetTitleArguments()
			=> new[] { Reference.GetTitle() ?? Reference.GetId().ToString() };

		public async UniTask<Texture2D> GetImage()
			=> await Main.Instance.NetworkAPI.FetchTexture(Reference.GetThumbnailUrl());

		public void OnClick(int menuId) { }
		// => Client.UiAPI?.SendGoto(menuId, AvatarPage.GetStaticKey(), "avatar", Reference);
	}
}