using Cysharp.Threading.Tasks;

namespace Nox.Tables {
	public interface ITableAPI {
		/// <summary>
		/// Gets the entry from the table by key.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="from"></param>
		/// <returns></returns>
		public UniTask<IEntry> Get(string key, string from = null);

		/// <summary>
		/// Sets the entry in the table with the specified key and value.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <param name="from"></param>
		/// <returns></returns>
		public UniTask<IEntry> Set(string key, string value, string from = null);

		/// <summary>
		/// Deletes the entry from the table by key.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="from"></param>
		/// <returns></returns>
		public UniTask<IEntry> Delete(string key, string from = null);
	}
}