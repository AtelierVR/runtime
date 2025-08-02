using UnityEngine;
using UnityEngine.UI;
using Nox.CCK.Utils;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace Nox.UI.Examples {
	/// <summary>
	/// Example script showing how to use the Keyboard MonoBehaviour
	/// Demonstrates setup, layout switching, and event handling
	/// </summary>
	public class KeyboardExample : MonoBehaviour {
		[Header("Keyboard Configuration")]
		[Tooltip("The keyboard component to control")]
		[SerializeField] private Keyboard keyboard;
		
		[Header("Layout Examples")]
		[Tooltip("QWERTY layout component")]
		[SerializeField] private QwertyKeyboardLayout qwertyLayout;
		
		[Tooltip("Numeric layout component")]
		[SerializeField] private NumericKeyboardLayout numericLayout;

		[Header("UI Elements")]
		[Tooltip("Input field to display keyboard output")]
		[SerializeField] private InputField outputField;
		
		[Tooltip("Text component to show current keyboard info")]
		[SerializeField] private Text infoText;

		[Header("Control Buttons")]
		[SerializeField] private Button showKeyboardButton;
		[SerializeField] private Button hideKeyboardButton;
		[SerializeField] private Button toggleKeyboardButton;
		[SerializeField] private Button qwertyLayoutButton;
		[SerializeField] private Button numericLayoutButton;
		[SerializeField] private Button clearTextButton;

		// Private fields
		private string _lastLayoutName = "";

		// Unity lifecycle
		private void Start() {
			SetupKeyboard();
			SetupButtons();
			UpdateInfo();
		}

		private void Update() {
			// Update info if layout changed
			if (keyboard != null && keyboard.CurrentLayout != null) {
				string currentLayoutName = keyboard.CurrentLayout.GetLayoutName();
				if (currentLayoutName != _lastLayoutName) {
					_lastLayoutName = currentLayoutName;
					UpdateInfo();
				}
			}
		}

		// Setup methods
		private void SetupKeyboard() {
			if (keyboard == null) {
				Logger.LogError("No keyboard assigned to KeyboardExample");
				return;
			}

			// Set up keyboard events
			keyboard.OnKeyPressed.AddListener(OnKeyPressed);
			keyboard.OnKeyReleased.AddListener(OnKeyReleased);
			keyboard.OnTextChanged.AddListener(OnTextChanged);
			keyboard.OnSubmit.AddListener(OnSubmit);
			keyboard.OnKeyboardShown.AddListener(OnKeyboardShown);
			keyboard.OnKeyboardHidden.AddListener(OnKeyboardHidden);

			// Set initial layout
			if (qwertyLayout != null) {
				keyboard.SetLayout(qwertyLayout);
			}

			Logger.LogDebug("Keyboard example setup complete");
		}

		private void SetupButtons() {
			// Show/Hide buttons
			if (showKeyboardButton != null) {
				showKeyboardButton.onClick.AddListener(() => keyboard?.Show());
			}

			if (hideKeyboardButton != null) {
				hideKeyboardButton.onClick.AddListener(() => keyboard?.Hide());
			}

			if (toggleKeyboardButton != null) {
				toggleKeyboardButton.onClick.AddListener(() => keyboard?.Toggle());
			}

			// Layout buttons
			if (qwertyLayoutButton != null) {
				qwertyLayoutButton.onClick.AddListener(SwitchToQwerty);
			}

			if (numericLayoutButton != null) {
				numericLayoutButton.onClick.AddListener(SwitchToNumeric);
			}

			// Clear button
			if (clearTextButton != null) {
				clearTextButton.onClick.AddListener(() => {
					keyboard?.ClearText();
					if (outputField != null) {
						outputField.text = "";
					}
				});
			}
		}

		// Button event handlers
		private void SwitchToQwerty() {
			if (keyboard != null && qwertyLayout != null) {
				keyboard.SetLayout(qwertyLayout);
				keyboard.SetInputMode("text");
				Logger.LogDebug("Switched to QWERTY layout");
			}
		}

		private void SwitchToNumeric() {
			if (keyboard != null && numericLayout != null) {
				keyboard.SetLayout(numericLayout);
				keyboard.SetInputMode("numeric");
				Logger.LogDebug("Switched to Numeric layout");
			}
		}

		// Keyboard event handlers
		private void OnKeyPressed(string key) {
			Logger.LogDebug($"Example: Key pressed - {key}");
			UpdateInfo();
		}

		private void OnKeyReleased(string key) {
			Logger.LogDebug($"Example: Key released - {key}");
		}

		private void OnTextChanged(string newText) {
			// Update output field
			if (outputField != null) {
				outputField.text = newText;
			}

			Logger.LogDebug($"Example: Text changed - '{newText}'");
			UpdateInfo();
		}

		private void OnSubmit(string finalText) {
			Logger.Log($"Example: Text submitted - '{finalText}'");
			
			// You could process the final text here
			// For example, send it to a chat system, save to file, etc.
		}

		private void OnKeyboardShown() {
			Logger.LogDebug("Example: Keyboard shown");
			UpdateInfo();
		}

		private void OnKeyboardHidden() {
			Logger.LogDebug("Example: Keyboard hidden");
			UpdateInfo();
		}

		// Helper methods
		private void UpdateInfo() {
			if (infoText == null || keyboard == null) return;

			string info = "Keyboard Status:\n";
			info += $"Visible: {keyboard.IsVisible}\n";
			info += $"Layout: {keyboard.CurrentLayout?.GetLayoutName() ?? "None"}\n";
			info += $"Input Mode: {keyboard.InputMode}\n";
			info += $"Current Text: '{keyboard.CurrentText}'\n";
			info += $"Text Length: {keyboard.CurrentText.Length}";
			
			if (keyboard.MaxInputLength > 0) {
				info += $"/{keyboard.MaxInputLength}";
			}

			infoText.text = info;
		}

		// Public methods for external control
		/// <summary>
		/// Create a simple keyboard setup programmatically
		/// </summary>
		/// <param name="parent">Parent transform for the keyboard</param>
		/// <returns>Created keyboard GameObject</returns>
		public static GameObject CreateSimpleKeyboard(Transform parent = null) {
			// Create keyboard GameObject
			GameObject keyboardObj = new GameObject("SimpleKeyboard");
			if (parent != null) {
				keyboardObj.transform.SetParent(parent, false);
			}

			// Add RectTransform
			var rectTransform = keyboardObj.AddComponent<RectTransform>();
			rectTransform.sizeDelta = new Vector2(600, 400);

			// Add Canvas components for UI
			var canvas = keyboardObj.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.WorldSpace;
			
			var canvasScaler = keyboardObj.AddComponent<CanvasScaler>();
			canvasScaler.dynamicPixelsPerUnit = 10f;

			keyboardObj.AddComponent<GraphicRaycaster>();

			// Create key container
			GameObject containerObj = new GameObject("KeyContainer");
			containerObj.transform.SetParent(keyboardObj.transform, false);
			
			var containerRect = containerObj.AddComponent<RectTransform>();
			containerRect.anchorMin = Vector2.zero;
			containerRect.anchorMax = Vector2.one;
			containerRect.sizeDelta = Vector2.zero;
			containerRect.anchoredPosition = Vector2.zero;

			// Add Keyboard component
			var keyboard = keyboardObj.AddComponent<Keyboard>();

			// Create and add QWERTY layout
			var qwertyLayoutObj = new GameObject("QwertyLayout");
			qwertyLayoutObj.transform.SetParent(keyboardObj.transform, false);
			var qwertyLayout = qwertyLayoutObj.AddComponent<QwertyKeyboardLayout>();

			// Configure keyboard
			keyboard.GetType().GetField("keyContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				?.SetValue(keyboard, containerObj.transform);
			
			keyboard.GetType().GetField("layoutComponent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				?.SetValue(keyboard, qwertyLayout);

			// Initialize keyboard
			keyboard.InitializeKeyboard();

			Logger.LogDebug("Simple keyboard created programmatically");
			return keyboardObj;
		}

		/// <summary>
		/// Demo method showing various keyboard features
		/// </summary>
		public void RunDemo() {
			if (keyboard == null) {
				Logger.LogWarning("No keyboard available for demo");
				return;
			}

			StartCoroutine(DemoCoroutine());
		}

		private System.Collections.IEnumerator DemoCoroutine() {
			Logger.Log("Starting keyboard demo...");

			// Show keyboard
			keyboard.Show();
			yield return new WaitForSeconds(1f);

			// Switch to QWERTY and type some text
			SwitchToQwerty();
			yield return new WaitForSeconds(0.5f);
			
			keyboard.SetText("Hello World!");
			yield return new WaitForSeconds(2f);

			// Clear and switch to numeric
			keyboard.ClearText();
			yield return new WaitForSeconds(0.5f);
			
			SwitchToNumeric();
			yield return new WaitForSeconds(0.5f);
			
			keyboard.SetText("12345");
			yield return new WaitForSeconds(2f);

			// Hide keyboard
			keyboard.Hide();
			yield return new WaitForSeconds(1f);

			Logger.Log("Keyboard demo complete!");
		}
	}
}
