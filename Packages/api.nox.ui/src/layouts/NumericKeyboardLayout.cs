using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nox.CCK.Utils;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace Nox.UI {
	/// <summary>
	/// Numeric keyboard layout for number input
	/// Optimized for numeric input, PIN entry, and calculator-style interfaces
	/// </summary>
	public class NumericKeyboardLayout : MonoBehaviour, IKeyboardLayout {
		[Header("Layout Settings")]
		[Tooltip("Key prefab to use for number keys")]
		[SerializeField] private GameObject keyPrefab;
		
		[Tooltip("Special key prefab for action keys")]
		[SerializeField] private GameObject specialKeyPrefab;
		
		[Tooltip("Key size for all keys")]
		[SerializeField] private Vector2 keySize = new Vector2(80, 80);

		[Header("Visual Settings")]
		[Tooltip("Color for number keys")]
		[SerializeField] private Color numberKeyColor = Color.white;
		
		[Tooltip("Color for action keys")]
		[SerializeField] private Color actionKeyColor = Color.gray;
		
		[Tooltip("Text color for keys")]
		[SerializeField] private Color textColor = Color.black;

		[Header("Layout Options")]
		[Tooltip("Include decimal point key")]
		[SerializeField] private bool includeDecimal = true;
		
		[Tooltip("Include negative/plus-minus key")]
		[SerializeField] private bool includeNegative = false;
		
		[Tooltip("Include basic math operators")]
		[SerializeField] private bool includeMathOperators = false;

		// Private fields
		private Keyboard _keyboard;
		private readonly List<GameObject> _createdKeys = new List<GameObject>();

		// Numeric layout definition (3x4 grid + additional keys)
		private string[][] _baseKeyRows = {
			new[] { "1", "2", "3" },
			new[] { "4", "5", "6" },
			new[] { "7", "8", "9" },
			new[] { "clear", "0", "backspace" },
			new[] { "enter" }
		};

		private readonly HashSet<string> _actionKeys = new HashSet<string> {
			"clear", "backspace", "enter", ".", "+/-", "+", "-", "*", "/"
		};

		// Interface implementation
		public string GetLayoutName() => "Numeric";

		public void Initialize(Keyboard keyboard) {
			_keyboard = keyboard;
			
			// Use keyboard's default prefabs if none are assigned
			if (keyPrefab == null) {
				keyPrefab = _keyboard.GetDefaultKeyPrefab();
			}
			if (specialKeyPrefab == null) {
				specialKeyPrefab = keyPrefab;
			}

			// Build the layout based on options
			BuildLayout();

			Logger.LogDebug("Numeric layout initialized");
		}

		public void CreateKeys(Transform container) {
			if (_keyboard == null) {
				Logger.LogError("Keyboard not initialized");
				return;
			}

			if (keyPrefab == null) {
				Logger.LogError("No key prefab available for numeric layout");
				return;
			}

			try {
				float spacing = _keyboard.GetKeySpacing();
				float yOffset = 0;

				for (int rowIndex = 0; rowIndex < _baseKeyRows.Length; rowIndex++) {
					var row = _baseKeyRows[rowIndex];
					float totalRowWidth = (row.Length * keySize.x) + ((row.Length - 1) * spacing);
					float startX = -totalRowWidth / 2f;

					for (int keyIndex = 0; keyIndex < row.Length; keyIndex++) {
						string keyValue = row[keyIndex];
						GameObject keyObj = CreateKey(keyValue, container);
						
						if (keyObj != null) {
							// Position the key
							var rectTransform = keyObj.GetComponent<RectTransform>();
							if (rectTransform != null) {
								rectTransform.anchoredPosition = new Vector2(
									startX + (keyIndex * (keySize.x + spacing)) + keySize.x / 2f,
									-yOffset - keySize.y / 2f
								);
								
								rectTransform.sizeDelta = keySize;
							}

							_createdKeys.Add(keyObj);
							_keyboard.RegisterKey(keyObj);
						}
					}

					yOffset += keySize.y + spacing;
				}

				Logger.LogDebug($"Created {_createdKeys.Count} keys for numeric layout");
			} catch (System.Exception e) {
				Logger.LogError($"Failed to create numeric keys: {e.Message}");
			}
		}

		public void OnKeyPressed(string key) {
			// Handle special numeric key behaviors
			switch (key) {
				case "clear":
					_keyboard?.ClearText();
					break;
				case "+/-":
					ToggleSign();
					break;
				default:
					Logger.LogDebug($"Numeric layout: Key '{key}' pressed");
					break;
			}
		}

		public void OnKeyReleased(string key) {
			Logger.LogDebug($"Numeric layout: Key '{key}' released");
		}

		public void Cleanup() {
			foreach (var key in _createdKeys) {
				if (key != null) {
					DestroyImmediate(key);
				}
			}
			_createdKeys.Clear();
			
			Logger.LogDebug("Numeric layout cleaned up");
		}

		public Vector2 GetPreferredSize() {
			float spacing = _keyboard?.GetKeySpacing() ?? 5f;
			
			// Calculate based on 3 columns and number of rows
			float width = (3 * keySize.x) + (2 * spacing);
			float height = (_baseKeyRows.Length * keySize.y) + ((_baseKeyRows.Length - 1) * spacing);
			
			return new Vector2(width, height);
		}

		public bool SupportsInputMode(string mode) {
			// Numeric layout supports numeric input modes
			return mode == "numeric" || mode == "decimal" || mode == "integer" || mode == "pin";
		}

		// Private helper methods
		private void BuildLayout() {
			// Start with base layout
			var keyRows = new List<string[]>();
			
			// Add main number rows
			keyRows.Add(new[] { "1", "2", "3" });
			keyRows.Add(new[] { "4", "5", "6" });
			keyRows.Add(new[] { "7", "8", "9" });

			// Build bottom row based on options
			var bottomRow = new List<string> { "clear", "0" };
			
			if (includeDecimal) {
				bottomRow.Insert(1, ".");
			}
			
			bottomRow.Add("backspace");
			keyRows.Add(bottomRow.ToArray());

			// Add additional rows based on options
			var actionRow = new List<string>();
			
			if (includeNegative) {
				actionRow.Add("+/-");
			}
			
			actionRow.Add("enter");
			
			if (includeMathOperators) {
				actionRow.AddRange(new[] { "+", "-", "*", "/" });
			}
			
			keyRows.Add(actionRow.ToArray());

			_baseKeyRows = keyRows.ToArray();
		}

		private GameObject CreateKey(string keyValue, Transform parent) {
			try {
				bool isActionKey = _actionKeys.Contains(keyValue);
				GameObject prefab = isActionKey ? specialKeyPrefab : keyPrefab;
				
				if (prefab == null) {
					Logger.LogWarning($"No prefab available for key: {keyValue}");
					return null;
				}

				GameObject keyObj = Instantiate(prefab, parent);
				keyObj.name = $"NumKey_{keyValue}";

				// Set up the key appearance
				SetupKeyAppearance(keyObj, keyValue, isActionKey);
				
				// Set up the key interaction
				SetupKeyInteraction(keyObj, keyValue);

				return keyObj;
			} catch (System.Exception e) {
				Logger.LogError($"Failed to create numeric key '{keyValue}': {e.Message}");
				return null;
			}
		}

		private void SetupKeyAppearance(GameObject keyObj, string keyValue, bool isActionKey) {
			// Set up visual appearance
			var image = keyObj.GetComponent<Image>();
			if (image != null) {
				image.color = isActionKey ? actionKeyColor : numberKeyColor;
			}

			// Set up text display
			var textComponent = keyObj.GetComponentInChildren<Text>();
			if (textComponent == null) {
				var tmpText = keyObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
				if (tmpText != null) {
					tmpText.text = GetDisplayText(keyValue);
					tmpText.color = textColor;
				}
			} else {
				textComponent.text = GetDisplayText(keyValue);
				textComponent.color = textColor;
			}
		}

		private void SetupKeyInteraction(GameObject keyObj, string keyValue) {
			var button = keyObj.GetComponent<Button>();
			if (button != null) {
				button.onClick.RemoveAllListeners();
				button.onClick.AddListener(() => {
					_keyboard?.ProcessKeyPress(keyValue);
				});
			} else {
				Logger.LogWarning($"Numeric key '{keyValue}' does not have a Button component");
			}
		}

		private string GetDisplayText(string keyValue) {
			switch (keyValue) {
				case "clear":
					return "C";
				case "backspace":
					return "⌫";
				case "enter":
					return "⏎";
				case "+/-":
					return "±";
				default:
					return keyValue;
			}
		}

		private void ToggleSign() {
			if (_keyboard == null) return;
			
			string currentText = _keyboard.CurrentText;
			if (string.IsNullOrEmpty(currentText)) return;

			if (currentText.StartsWith("-")) {
				// Remove negative sign
				_keyboard.SetText(currentText.Substring(1));
			} else {
				// Add negative sign
				_keyboard.SetText("-" + currentText);
			}
		}

		// Unity lifecycle
		private void OnValidate() {
			if (keySize.x <= 0) keySize.x = 80;
			if (keySize.y <= 0) keySize.y = 80;
		}
	}
}
