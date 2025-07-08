using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Tables;

namespace api.nox.table {
	public class Entry : IEntry, INoxObject {
		public string Server;
		public string Key;
		public string Value;

		public string GetServerAddress()
			=> Server;

		public string GetKey()
			=> Key;

		public string GetValue()
			=> Value;

		public async UniTask<IEntry> Refresh()
			=> await Main.Instance.Network.Get(Key, Server);

		public async UniTask<IEntry> Update(string v)
			=> await Main.Instance.Network.Set(Key, v, Server);

		public UniTask Delete()
			=> Main.Instance.Network.Delete(Key, Server);
	}
}