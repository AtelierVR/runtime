using api.nox.user.client;
using Cysharp.Threading.Tasks;
using Nox.Search;
using UnityEngine;

namespace api.nox.user.search {
	public class SearchData : IResultData {
		public User Reference;

		public int Id
			=> Reference.ToIdentifier().ToString().GetHashCode();

		public string[] TitleArguments
			=> new[] { Reference.GetDisplay() ?? Reference.GetUsername() };

		public UniTask<Texture2D> Image
			=> Reference.GetThumbnail();

		public void OnClick(int menuId)
			=> Client.UiAPI?.SendGoto(menuId, UserPage.GetStaticKey(), "user", Reference);
	}
}