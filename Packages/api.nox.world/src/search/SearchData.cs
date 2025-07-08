using api.nox.world.client;
using Cysharp.Threading.Tasks;
using Nox.Search;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.world.search {
	public class SearchData : IResultData {
		public World Reference;

		public int GetId()
			=> Reference.ToIdentifier().ToString().GetHashCode();

		public string GetTitleKey()
			=> "world.search.data.title";

		public string[] GetTitleArguments()
			=> new[] { Reference.GetTitle() ?? Reference.GetId().ToString() };

		public async UniTask<Texture2D> GetImage()
			=> await Main.Instance.NetworkAPI.FetchTexture(Reference.GetThumbnailUrl());

		public void OnClick(int menuId)
			=> Client.UiAPI?.SendGoto(menuId, WorldPage.GetStaticKey(), "world", Reference);
	}
}