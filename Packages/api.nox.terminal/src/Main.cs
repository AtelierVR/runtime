using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.Terminals;

namespace api.nox.terminal {
	public class Main : MainModInitializer, ITerminalAPI {
		internal        MainModCoreAPI CoreAPI;
		internal static Main           Instance;
		private         CommandManager _manager;

		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			_manager = new CommandManager();
		}

		public void OnDisposeMain() {
			CoreAPI  = null;
			Instance = null;
			_manager = null;
		}

		public bool Execute(string args)
			=> _manager.ExecuteCommand(args);

		public string[] AutoComplete(string args)
			=> _manager.AutoComplete(args);

		public uint Register(ICommand command)
			=> _manager.Register(command);

		public void Unregister(uint id)
			=> _manager.Unregister(id);
	}
}