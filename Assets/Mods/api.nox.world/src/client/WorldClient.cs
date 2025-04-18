using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace api.nox.world
{
    public class WorldClient : ClientModInitializer
    {
        internal static WorldClient Instance;

        internal static MainModInitializer UISystem
            => WorldSystem.CoreAPI.ModAPI
                .GetMod("ui").GetMains()
                .FirstOrDefault();
        
        internal static MainModInitializer NetworkAPI
            => WorldSystem.CoreAPI.ModAPI
                .GetMod("network").GetMains()
                .FirstOrDefault();

        private HomeWidget _homeWidget;

        internal readonly UnityEvent<INoxObject> OnHomeUpdated = new();

        private EventSubscription[] _events;

        public async UniTask OnInitializeClientAsync(ClientModCoreAPI api)
        {
            Instance = this;
            _homeWidget = new HomeWidget();
            var user = NetworkAPI.GetField("User").CallMethod("GetCurrentUser");
            user ??= await NetworkAPI.GetField("User").CallAsyncMethod("GetMyUser");
            await OnUserUpdated(user);
            _events = new[]
            {
                WorldSystem.CoreAPI.EventAPI.Subscribe("user_update", ctx => OnUserUpdated(ctx).Forget()),
                WorldSystem.CoreAPI.EventAPI.Subscribe("home_update",
                    data => OnHomeUpdated.Invoke(data.TryGet(0, out INoxObject home)
                        ? home
                        : null
                    ))
            };
        }

        private async UniTask OnUserUpdated(EventData data)
        {
            if (!data.TryGet(0, out INoxObject user)) return;
            await OnUserUpdated(user);
        }

        private async UniTask OnUserUpdated(INoxObject user)
        {
            if (user == null || string.IsNullOrEmpty(user.GetField<string>("home"))) return;
            var home = await user.CallAsyncMethod("GetHome");
            if (home == null) return;
            OnHomeUpdated.Invoke(home);
        }


        public void OnDisposeClient()
        {
            _homeWidget.Dispose();
            foreach (var subscription in _events)
                WorldSystem.CoreAPI.EventAPI.Unsubscribe(subscription);
            _homeWidget = null;
            Instance = null;
        }
    }
}