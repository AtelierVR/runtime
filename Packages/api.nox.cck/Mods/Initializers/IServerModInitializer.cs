using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers {
	public interface IServerModInitializer : IModInitializer {
		public void OnInitializeServer(ServerModCoreAPI api) { }

		public UniTask OnInitializeServerAsync(ServerModCoreAPI api)
			=> UniTask.CompletedTask;

		public void OnPostInitializeServer() { }

		public UniTask OnPostInitializeServerAsync()
			=> UniTask.CompletedTask;

		public void OnUpdateServer()      { }
		public void OnLateUpdateServer()  { }
		public void OnFixedUpdateServer() { }

		public void OnPreDisposeServer() { }

		public UniTask OnPreDisposeServerAsync()
			=> UniTask.CompletedTask;

		public void OnDisposeServer() { }

		public UniTask OnDisposeServerAsync()
			=> UniTask.CompletedTask;
	}
}