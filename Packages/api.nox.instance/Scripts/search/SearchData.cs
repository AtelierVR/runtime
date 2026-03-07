using api.nox.instance.client;
using Cysharp.Threading.Tasks;
using Nox.Search;
using UnityEngine;

namespace api.nox.instance.search {
	public class SearchData : IResultData {
		public Instance Reference;

		public int Id
			=> Reference.ToIdentifier().ToString().GetHashCode();

		public string[] TitleArguments
			=> new[] { Reference.GetTitle() ?? Reference.GetId().ToString() };

		public UniTask<Texture2D> Image
			=> Main.NetworkAPI.FetchTexture(Reference.GetThumbnailUrl());

		public void OnClick(int menuId)
			=> Client.UiAPI?.SendGoto(menuId, InstancePage.GetStaticKey(), "instance", Reference);
	}
}