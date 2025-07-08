using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers
{
    public interface MainModInitializer : IModInitializer
    {
        public void OnInitializeMain(MainModCoreAPI api) { }
        public UniTask OnInitializeMainAsync(MainModCoreAPI api) => UniTask.CompletedTask;
        public void OnPostInitializeMain() { }
        public UniTask OnPostInitializeMainAsync() => UniTask.CompletedTask;
        
        public void OnUpdateMain() { }
        public void OnLateUpdateMain() { }
        public void OnFixedUpdateMain() { }

        public void OnPreDisposeMain() { }
        public UniTask OnPreDisposeMainAsync() => UniTask.CompletedTask;
        public void OnDisposeMain() { }
        public UniTask OnDisposeMainAsync() => UniTask.CompletedTask;
    }
}