namespace Nox.Terminals {
	public interface ITerminalAPI {
		public bool Execute(string args);

		public string[] AutoComplete(string args);

		public uint Register(ICommand command);

		public void Unregister(uint id);
	}
}