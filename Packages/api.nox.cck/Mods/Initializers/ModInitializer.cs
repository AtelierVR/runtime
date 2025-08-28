using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Utils;

namespace Nox.CCK.Mods.Initializers
{
    public interface IModInitializer : INoxObject
    {
        public void OnInitialize(IModCoreAPI api) { }
        public UniTask OnInitializeAsync(IModCoreAPI api) => UniTask.CompletedTask;
        public void OnPostInitialize() { }
        public UniTask OnPostInitializeAsync() => UniTask.CompletedTask;
        
        public void OnUpdate() { }
        public void OnLateUpdate() { }
        public void OnFixedUpdate() { }
        public void OnPreDispose() { }
        public UniTask OnPreDisposeAsync() => UniTask.CompletedTask;
        public void OnDispose() { }
        public UniTask OnDisposeAsync() => UniTask.CompletedTask;
    }
}