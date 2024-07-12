using Cysharp.Threading.Tasks;
using UnityEngine.PlayerLoop;

namespace api.nox.game
{
    internal class UserNav
    {
        internal NavigationTileManager navigationTile;
        private NavigationHandler _handler;

        public UserNav(NavigationTileManager navigationTile)
        {
            this.navigationTile = navigationTile;
            GenerateHandler();
            Initialization().Forget();
        }

        private async UniTask Initialization()
        {
            await UniTask.DelayFrame(1);
            UpdateHandler();
        }

        private void GenerateHandler()
        {
            _handler = new NavigationHandler
            {
                id = "api.nox.game.navigation.user",
                text_key = "dashboard.navigation.user",
                title_key = "dashboard.navigation.user.title",
                GetWorkers = () => new NavigationWorker[0]
            };
        }

        private void UpdateHandler()
        {
            navigationTile.clientMod.coreAPI.EventAPI.Emit("game.navigation", _handler);
        }

        internal void OnDispose()
        {
            _handler.GetWorkers = null;
            UpdateHandler();
            _handler = null;
        }
    }
}