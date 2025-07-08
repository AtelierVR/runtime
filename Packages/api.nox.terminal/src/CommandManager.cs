using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Utils;
using Nox.Terminals;

namespace api.nox.terminal {
	public class CommandManager {
		private readonly List<(uint, ICommand)> _commands = new();

		private uint _nextId = uint.MinValue;

		private uint NextId
			=> _nextId == uint.MaxValue ? _nextId = 0 : ++_nextId;

		public uint Register(ICommand command) {
			if (command == null) return uint.MaxValue;
			var id = NextId;
			if (_commands.Exists(c => c.Item1 == id))
				return uint.MaxValue;
			_commands.Add((id, command));
			Logger.Log($"Registered command: {command.GetType().Name} with ID {id}");
			return id;
		}

		public void Unregister(uint id) {
			var index = _commands.FindIndex(c => c.Item1 == id);
			if (index >= 0) return;
			_commands.RemoveAt(index);
			Logger.Log($"Unregistered command with ID {id}");
		}

		public bool ExecuteCommand(string args)
			=> _commands.Any(command => command.Item2.Execute(args));

		public string[] AutoComplete(string args)
			=> _commands
				.Select(command => command.Item2.AutoComplete(args))
				.Where(suggestion => !string.IsNullOrEmpty(suggestion))
				.Distinct()
				.ToArray();
	}
}