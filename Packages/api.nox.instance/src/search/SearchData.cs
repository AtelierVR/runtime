using api.nox.instance.client;
using Cysharp.Threading.Tasks;
using Nox.Search;
using UnityEngine;

namespace api.nox.instance.search {
	public class SearchData : IResultData {
		public Instance Reference;

		public int GetId()
			=> Reference.ToIdentifier().ToString().GetHashCode();

		public string GetTitleKey()
			=> "instance.search.data.title";

		public string[] GetTitleArguments()
			=> new[] { Reference.GetTitle() ?? Reference.GetId().ToString() };

		public async UniTask<Texture2D> GetImage()
			=> await Main.Instance.NetworkAPI.FetchTexture(Reference.GetThumbnailUrl());

		public void OnClick(int menuId)
			=> Client.UiAPI?.SendGoto(menuId, InstancePage.GetStaticKey(), "instance", Reference);
	}
}