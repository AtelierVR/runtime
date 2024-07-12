using Cysharp.Threading.Tasks;
using UnityEngine.PlayerLoop;

namespace api.nox.game
{
    internal class WorldNav
    {
        internal NavigationTileManager navigationTile;
        private NavigationHandler _handler;

        public WorldNav(NavigationTileManager navigationTile)
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
                id = "api.nox.game.navigation.world",
                text_key = "dashboard.navigation.world",
                title_key = "dashboard.navigation.world.title",
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