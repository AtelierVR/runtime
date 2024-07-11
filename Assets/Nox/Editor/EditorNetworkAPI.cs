using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Networks;
using Nox.CCK.Users;
using UnityEngine;

namespace Nox.Editor.Mods
{
    public class EditorNetworkAPI : NetworkAPI
    {
        private EditorMod _mod;
        internal EditorNetworkAPI(EditorMod mod)
        {
            _mod = mod;
        }

        internal EditorMod GetNetworkMod() => _mod?.coreAPI?.EditorModAPI?.GetEditorMod("network");

        internal NetworkAPI GetNetworkAPI()
        {
            var mainclass = GetNetworkMod().GetMainClasses();
            if (mainclass == null) return null;
            foreach (var mc in mainclass)
                if (mc is NetworkAPI)
                    return mc as NetworkAPI;
            return null;
        }

        public NetworkAPIWorld WorldAPI => GetNetworkAPI().WorldAPI;
        public UserMe GetCurrentUser() => GetNetworkAPI().GetCurrentUser();
        public NetworkAPIUser UserAPI => GetNetworkAPI().UserAPI;

        public override string ToString() => $"{GetType().Name}[Mod={_mod.GetMetadata().GetId()}, APIs={((UserAPI?.GetType().Name ?? "null") + (WorldAPI?.GetType().Name ?? "null"))}]";

        public async UniTask<Texture2D> FetchTexture(string url) => await GetNetworkAPI().FetchTexture(url);
    }
}