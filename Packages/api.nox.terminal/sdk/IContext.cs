using System.Collections.Generic;
using UnityEngine;

namespace Nox.Terminals {
	public interface IContext {
		/// <summary>
		/// Get the id of the context.
		/// </summary>
		/// <returns></returns>
		public int GetId();

		/// <summary>
		/// Get environment variables for the context.
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, string> GetEnvironments();

		/// <summary>
		/// Get an environment variable by key.
		/// If the value is null, the key will be removed from the environment.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void SetEnvironment(string key, string value);

		/// <summary>
		/// Add text to the terminal output.
		/// </summary>
		/// <param name="message"></param>
		public void Print(string message);

		/// <summary>
		/// Add text to the terminal output with a newline.
		/// </summary>
		/// <param name="message"></param>
		public void PrintLn(string message);

		/// <summary>
		/// Clear the terminal output.
		/// </summary>
		public void Clear();
	}
}