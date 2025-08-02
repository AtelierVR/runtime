using UnityEngine;

namespace Nox.UI {
	/// <summary>
	/// Interface for keyboard layouts that define the arrangement and behavior of keys
	/// </summary>
	public interface IKeyboardLayout {
		/// <summary>
		/// The name/identifier of the layout (e.g., "QWERTY", "AZERTY", "Numeric")
		/// </summary>
		string GetLayoutName();

		/// <summary>
		/// Initialize the layout with the keyboard instance
		/// </summary>
		/// <param name="keyboard">The keyboard MonoBehaviour that will use this layout</param>
		void Initialize(Keyboard keyboard);

		/// <summary>
		/// Create and arrange the keys according to this layout
		/// </summary>
		/// <param name="container">The container where keys should be instantiated</param>
		void CreateKeys(Transform container);

		/// <summary>
		/// Handle key press events
		/// </summary>
		/// <param name="key">The key that was pressed</param>
		void OnKeyPressed(string key);

		/// <summary>
		/// Handle key release events
		/// </summary>
		/// <param name="key">The key that was released</param>
		void OnKeyReleased(string key);

		/// <summary>
		/// Clean up the layout
		/// </summary>
		void Cleanup();

		/// <summary>
		/// Get the preferred size for the keyboard with this layout
		/// </summary>
		/// <returns>Preferred size in pixels</returns>
		Vector2 GetPreferredSize();

		/// <summary>
		/// Check if this layout supports the specified input mode
		/// </summary>
		/// <param name="mode">Input mode (e.g., "text", "numeric", "password")</param>
		/// <returns>True if the layout supports this mode</returns>
		bool SupportsInputMode(string mode);
	}
}
