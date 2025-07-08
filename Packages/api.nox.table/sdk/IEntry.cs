using Cysharp.Threading.Tasks;

namespace Nox.Tables {
	public interface IEntry {
		/// <summary>
		/// Gets the server address where the entry is stored.
		/// </summary>
		/// <returns></returns>
		public string GetServerAddress();

		/// <summary>
		/// Gets the key.
		/// </summary>
		/// <returns></returns>
		public string GetKey();

		/// <summary>
		/// Gets the value.
		/// </summary>
		/// <returns></returns>
		public string GetValue();

		/// <summary>
		/// Refreshes the table data.
		/// </summary>
		/// <returns></returns>
		public UniTask<IEntry> Refresh();

		/// <summary>
		/// Updates the table with a new value.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public UniTask<IEntry> Update(string value);

		/// <summary>
		/// Deletes the table entry.
		/// </summary>
		/// <returns></returns>
		public UniTask Delete();
	}
}