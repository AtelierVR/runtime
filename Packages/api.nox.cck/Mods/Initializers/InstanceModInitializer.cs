using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers
{
    public interface InstanceModInitializer : IModInitializer
    {
        public void OnInitializeInstance(InstanceModCoreAPI api) { }
        public UniTask OnInitializeInstanceAsync(InstanceModCoreAPI api) => UniTask.CompletedTask;
        public void OnPostInitializeInstance() { }
        public UniTask OnPostInitializeInstanceAsync() => UniTask.CompletedTask;

        public void OnUpdateInstance() { }
        public void OnLateUpdateInstance() { }
        public void OnFixedUpdateInstance() { }

        public void OnPreDisposeInstance() { }
        public UniTask OnPreDisposeInstanceAsync() => UniTask.CompletedTask;
        public void OnDisposeInstance() { }
        public UniTask OnDisposeInstanceAsync() => UniTask.CompletedTask;
    }
}