using System;
using Nox.Terminals;

namespace api.nox.session.commands {
	public class Commands : IDisposable {
		internal static ITerminalAPI TerminalAPI
			=> Main.Instance.CoreAPI.ModAPI
				.GetMod("terminal")
				.GetInstance<ITerminalAPI>();

		private readonly (uint, ICommand)[] _list = {
			(0, new RespawnCommand()),
			(0, new PlayerListCommand())
		};

		public Commands() {
			for (var i = 0; i < _list.Length; i++)
				_list[i] = (TerminalAPI.Register(_list[i].Item2), _list[i].Item2);
		}

		public void Dispose() {
			for (var i = 0; i < _list.Length; i++)
				TerminalAPI.Unregister(_list[i].Item1);
		}
	}
}