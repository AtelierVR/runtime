namespace Nox.Terminals {
	public interface ICommand {
		/// <summary>
		/// Get new arguments for the command.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public string AutoComplete(string input);

		/// <summary>
		/// Executes the command with the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <returns>if the command was executed.</returns>
		public bool Execute(string input);
	}
}