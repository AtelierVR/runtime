using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.Search {
	public interface IResultData {
		public int GetId();

		public string GetTitleKey();

		public string[] GetTitleArguments();

		public UniTask<Texture2D> GetImage();

		public void OnClick();
	}
}