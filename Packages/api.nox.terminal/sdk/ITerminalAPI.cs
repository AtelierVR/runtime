using Cysharp.Threading.Tasks;

namespace Nox.Terminals {
	public interface ITerminalAPI {
		public string GetPrefix();

		public ICommand[] GetRegistered();

		public UniTask<bool> Execute(string args, IContext context = null);

		public string[] AutoComplete(string args, IContext context = null);

		public uint Register(ICommand command);

		public void Unregister(uint id);
	}
}