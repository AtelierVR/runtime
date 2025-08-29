using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.Terminals;

namespace api.nox.terminal.commands {
	public class TestCommand : ICommand, IHelper {
		public string GetName()
			=> "test";

		public string GetDescription()
			=> LanguageManager.Get($"terminal.command.{GetName()}.description");
		
		public string GetShort()
			=> LanguageManager.Get($"terminal.command.{GetName()}.short");

		public string GetUsage()
			=> $"{CommandWithPrefix} <url>";

		private string CommandWithPrefix
			=> $"{CommandManager.CommandPrefix}{GetName()}";

		public string[] AutoComplete(string input, IContext context = null)
			=> CommandWithPrefix.StartsWith(input.ToLower())
				? new[] { CommandWithPrefix }
				: Array.Empty<string>();

		public UniTask<bool> Execute(string input, IContext context = null) {
			if (input.ToLower() != CommandWithPrefix)
				return UniTask.FromResult(false);

			context?.PrintLn("Test command executed successfully!");
			return UniTask.FromResult(true);
		}
	}
}