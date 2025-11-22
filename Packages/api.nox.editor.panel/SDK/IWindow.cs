using System.Collections.Generic;
using Nox.CCK.Utils;

namespace Nox.Editor.Panel {
	public interface IWindow {
		/// <summary>
		/// Get the active instance in this window.
		/// </summary>
		/// <returns></returns>
		public IInstance GetActive();

		/// <summary>
		/// Set the active instance in this window.
		/// </summary>
		/// <param name="panel"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public bool SetActive(IPanel panel, Dictionary<string, object> data = null);

		/// <summary>
		/// Close this window.
		/// </summary>
		public void Close();

		/// <summary>
		/// Focus this window.
		/// </summary>
		public void Focus();
		
		/// <summary>
		/// Repaint this window.
		/// </summary>
		public void Repaint();
		
		/// <summary>
		/// Show this window.
		/// </summary>
		public void Show();
	}
}