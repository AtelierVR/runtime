using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers {
	public interface IClientModInitializer : IModInitializer {
		public void OnInitializeClient(ClientModCoreAPI api) { }

		public UniTask OnInitializeClientAsync(ClientModCoreAPI api)
			=> UniTask.CompletedTask;

		public void OnPostInitializeClient() { }

		public UniTask OnPostInitializeClientAsync()
			=> UniTask.CompletedTask;

		public void OnUpdateClient()      { }
		public void OnLateUpdateClient()  { }
		public void OnFixedUpdateClient() { }

		public void OnPreDisposeClient() { }

		public UniTask OnPreDisposeClientAsync()
			=> UniTask.CompletedTask;

		public void OnDisposeClient() { }

		public UniTask OnDisposeClientAsync()
			=> UniTask.CompletedTask;
	}
}