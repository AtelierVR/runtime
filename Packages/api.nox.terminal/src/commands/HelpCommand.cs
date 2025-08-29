using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.Terminals;

namespace api.nox.terminal.commands {
	public class HelpCommand : ICommand, IHelper {
		public string GetName()
			=> "help";

		public string GetDescription()
			=> LanguageManager.Get($"terminal.command.{GetName()}.description");

		public string GetShort()
			=> LanguageManager.Get($"terminal.command.{GetName()}.short");

		public string GetUsage()
			=> $"{CommandWithPrefix} [<command>]";

		private string CommandWithPrefix
			=> $"{CommandManager.CommandPrefix}{GetName()}";

		public string[] AutoComplete(string input, IContext context = null)
			=> CommandWithPrefix.StartsWith(input.ToLower())
				? new[] { CommandWithPrefix }
				: Array.Empty<string>();

		public UniTask<bool> Execute(string input, IContext context = null)
			=> UniTask.FromResult(ExecuteInternal(input, context));

		private bool ExecuteInternal(string input, IContext context = null) {
			if (string.IsNullOrWhiteSpace(input) || context == null)
				return false;

			var parts = input.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length > 2 || !parts[0].Equals(CommandWithPrefix, StringComparison.OrdinalIgnoreCase))
				return false;

			var commandName = parts.Length == 2 ? parts[1] : null;
			var commands    = Main.Instance.GetRegistered();

			if (!string.IsNullOrEmpty(commandName)) {
				if (commands.FirstOrDefault(c => c is IHelper ch && ch.GetName().Equals(commandName, StringComparison.OrdinalIgnoreCase)) is not IHelper command) {
					context.PrintLn(LanguageManager.Get("terminal.command.help.no_help", commands.Length));
					return true;
				}

				context.PrintLn(
					LanguageManager.Get(
						"terminal.command.help.content",
						new object[] {
							command.GetName(),
							command.GetDescription(),
							command.GetUsage()
						}
					)
				);
				return true;
			}

			context.PrintLn(LanguageManager.Get("terminal.command.help.list_header"));
			foreach (var command in commands) {
				if (command is not IHelper ch) continue;
				context.PrintLn(LanguageManager.Get("terminal.command.help.list_item", new object[] { ch.GetName(), ch.GetShort() }));
			}

			return true;
		}
	}
}