using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Metadata;

namespace Nox.CCK.Mods.Mods {
	public interface IModAPI {
		public IMod GetMod(string id);

		public IMod[] GetMods();

		public UniTask<IMod> LoadMod(string id);

		public UniTask<bool> UnloadMod(string id);

		public UniTask<bool> ReloadMod(string id);

		public ModMetadata GetMetadata(string id);

		public IMod GetSelf();
	}
}