using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.Network {
	public interface INetworkAPI {
		public string MergeUrl(Uri url, string path);

		public string MergeUrl(string url, string path);

		public UniTask<string> GetGateway(string address);

		public IRequest MakeRequest();

		public UniTask<Texture2D> FetchTexture(string address);
	}
}