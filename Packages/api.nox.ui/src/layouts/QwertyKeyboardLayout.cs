using System.Collections.Generic;
using Nox.CCK.Language;
using UnityEngine;
using UnityEngine.UI;
using Nox.CCK.Utils;
using Nox.UI;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace api.nox.ui {
	/// <summary>
	/// Basic QWERTY keyboard layout implementation
	/// Supports text input mode with standard QWERTY key arrangement
	/// </summary>
	public class QwertyKeyboardLayout : MonoBehaviour, IKeyboardLayout {
		[Header("Layout Settings")] [Tooltip("Key prefab to use for regular keys")] [SerializeField]
		private GameObject keyPrefab;

		[Tooltip("Special key prefab for larger keys (Space, Enter, etc.)")] [SerializeField]
		private GameObject specialKeyPrefab;

		[Tooltip("Key size for regular keys")] [SerializeField]
		private Vector2 keySize = new Vector2(60, 60);

		[Tooltip("Key size for special keys")] [SerializeField]
		private Vector2 specialKeySize = new Vector2(120, 60);

		[Header("Visual Settings")] [Tooltip("Color for regular keys")] [SerializeField]
		private Color regularKeyColor = Color.white;

		[Tooltip("Color for special keys (Shift, Enter, etc.)")] [SerializeField]
		private Color specialKeyColor = Color.gray;

		[Tooltip("Text color for keys")] [SerializeField]
		private Color textColor = Color.black;


		// Private fields
		private          Keyboard         _keyboard;
		private readonly List<GameObject> _createdKeys = new List<GameObject>();

		// QWERTY layout definition
		private readonly string[][] _keyRows = {
			new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "backspace" },
			new[] { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p" },
			new[] { "a", "s", "d", "f", "g", "h", "j", "k", "l", "enter" },
			new[] { "shift", "z", "x", "c", "v", "b", "n", "m", "shift" },
			new[] { "space" }
		};

		private readonly Dictionary<string, string> _shiftedKeys = new Dictionary<string, string> {
			{ "1", "!" }, { "2", "@" }, { "3", "#" }, { "4", "$" }, { "5", "%" },
			{ "6", "^" }, { "7", "&" }, { "8", "*" }, { "9", "(" }, { "0", ")" },
			{ "q", "Q" }, { "w", "W" }, { "e", "E" }, { "r", "R" }, { "t", "T" },
			{ "y", "Y" }, { "u", "U" }, { "i", "I" }, { "o", "O" }, { "p", "P" },
			{ "a", "A" }, { "s", "S" }, { "d", "D" }, { "f", "F" }, { "g", "G" },
			{ "h", "H" }, { "j", "J" }, { "k", "K" }, { "l", "L" },
			{ "z", "Z" }, { "x", "X" }, { "c", "C" }, { "v", "V" }, { "b", "B" },
			{ "n", "N" }, { "m", "M" }
		};

		private readonly HashSet<string> _specialKeys = new HashSet<string> {
			"backspace", "enter", "shift", "space", "tab", "capslock"
		};

		// Interface implementation
		public string GetLayoutName()
			=> "QWERTY";

		public void Initialize(Keyboard keyboard) {
			_keyboard = keyboard;

			// Use keyboard's default prefabs if none are assigned
			if (keyPrefab == null) {
				keyPrefab = _keyboard.GetDefaultKeyPrefab();
			}

			if (specialKeyPrefab == null) {
				specialKeyPrefab = keyPrefab; // Fallback to regular key prefab
			}

			Logger.LogDebug("QWERTY layout initialized");
		}

		public void CreateKeys(Transform container) {
			if (_keyboard == null) {
				Logger.LogError("Keyboard not initialized");
				return;
			}

			if (keyPrefab == null) {
				Logger.LogError("No key prefab available for QWERTY layout");
				return;
			}

			try {
				float spacing = _keyboard.GetKeySpacing();
				float yOffset = 0;

				for (int rowIndex = 0; rowIndex < _keyRows.Length; rowIndex++) {
					var   row     = _keyRows[rowIndex];
					float xOffset = 0;

					// Calculate row width for centering
					float rowWidth = CalculateRowWidth(row, spacing);
					float startX   = -rowWidth / 2f;

					for (int keyIndex = 0; keyIndex < row.Length; keyIndex++) {
						string     keyValue = row[keyIndex];
						GameObject keyObj   = CreateKey(keyValue, container);

						if (keyObj != null) {
							// Position the key
							var rectTransform = keyObj.GetComponent<RectTransform>();
							if (rectTransform != null) {
								Vector2 currentKeySize = _specialKeys.Contains(keyValue) ? specialKeySize : keySize;

								rectTransform.anchoredPosition = new Vector2(
									startX   + xOffset + currentKeySize.x / 2f,
									-yOffset - currentKeySize.y           / 2f
								);

								rectTransform.sizeDelta =  currentKeySize;
								xOffset                 += currentKeySize.x + spacing;
							}

							_createdKeys.Add(keyObj);
							_keyboard.RegisterKey(keyObj);
						}
					}

					yOffset += keySize.y + spacing;
				}

				Logger.LogDebug($"Created {_createdKeys.Count} keys for QWERTY layout");
			} catch (System.Exception e) {
				Logger.LogError($"Failed to create QWERTY keys: {e.Message}");
			}
		}

		public void OnKeyPressed(string key) {
			// Handle layout-specific key press logic
			Logger.LogDebug($"QWERTY layout: Key '{key}' pressed");
		}

		public void OnKeyReleased(string key) {
			// Handle layout-specific key release logic
			Logger.LogDebug($"QWERTY layout: Key '{key}' released");
		}

		public void Cleanup() {
			foreach (var key in _createdKeys) {
				if (key != null) {
					DestroyImmediate(key);
				}
			}

			_createdKeys.Clear();

			Logger.LogDebug("QWERTY layout cleaned up");
		}

		public Vector2 GetPreferredSize() {
			// Calculate preferred size based on layout
			float maxRowWidth = 0;
			float totalHeight = 0;
			float spacing     = _keyboard?.GetKeySpacing() ?? 5f;

			for (int i = 0; i < _keyRows.Length; i++) {
				float rowWidth = CalculateRowWidth(_keyRows[i], spacing);
				maxRowWidth =  Mathf.Max(maxRowWidth, rowWidth);
				totalHeight += keySize.y + spacing;
			}

			totalHeight -= spacing; // Remove last spacing
			return new Vector2(maxRowWidth, totalHeight);
		}

		public bool SupportsInputMode(string mode) {
			// QWERTY layout supports text input modes
			return mode == "text" || mode == "password" || mode == "email";
		}

		// Private helper methods
		private float CalculateRowWidth(string[] row, float spacing) {
			float width = 0;
			for (int i = 0; i < row.Length; i++) {
				string  key            = row[i];
				Vector2 currentKeySize = _specialKeys.Contains(key) ? specialKeySize : keySize;
				width += currentKeySize.x;
				if (i < row.Length - 1) {
					width += spacing;
				}
			}

			return width;
		}

		private GameObject CreateKey(string keyValue, Transform parent) {
			try {
				bool       isSpecialKey = _specialKeys.Contains(keyValue);
				GameObject prefab       = isSpecialKey ? specialKeyPrefab : keyPrefab;

				if (prefab == null) {
					Logger.LogWarning($"No prefab available for key: {keyValue}");
					return null;
				}

				GameObject keyObj = Instantiate(prefab, parent);
				keyObj.name = $"Key_{keyValue}";

				// Set up the key appearance
				SetupKeyAppearance(keyObj, keyValue, isSpecialKey);

				// Set up the key interaction
				SetupKeyInteraction(keyObj, keyValue);

				return keyObj;
			} catch (System.Exception e) {
				Logger.LogError($"Failed to create key '{keyValue}'");
				Logger.LogError(e);
				return null;
			}
		}

		private void SetupKeyAppearance(GameObject keyObj, string keyValue, bool isSpecialKey) {
			var image   = Reference.GetComponent<Image>("image", keyObj);
			var text    = Reference.GetComponent<TextLanguage>("text", keyObj);
			var display = GetDisplayText(keyValue);

			if (image && display.Item2 != null) {
				image.sprite = Main.Instance.CoreAPI.AssetAPI.GetAsset<Sprite>(display.Item2);
				image.gameObject.SetActive(true);
				text.gameObject.SetActive(false);
			} else {
				text.gameObject.SetActive(true);
				image.gameObject.SetActive(false);
			}

			text.UpdateText("key.value", new[] { display.Item1 });
		}

		private void SetupKeyInteraction(GameObject keyObj, string keyValue) {
			// Set up button interaction
			var button = keyObj.GetComponent<Button>();
			if (button != null) {
				button.onClick.RemoveAllListeners();
				button.onClick.AddListener(() => { _keyboard?.ProcessKeyPress(keyValue); });
			} else {
				Logger.LogWarning($"Key '{keyValue}' does not have a Button component");
			}
		}

		private (string, string) GetDisplayText(string keyValue) {
			// Return display text for the key
			switch (keyValue) {
				case "backspace":
					return (keyValue, "icons/backspace.png");
				case "enter":
					return (keyValue, "icons/keyboard_return.png");
				case "shift":
					return (keyValue, "icons/shift.png");
				case "space":
					return (keyValue, "icons/space_bar.png");
				case "tab":
					return (keyValue, "icons/keyboard_tab.png");
				case "capslock":
					return (keyValue, "icons/shift_lock.png");
				default:
					return (keyValue, null);
			}
		}

		// Unity lifecycle
		private void OnValidate() {
			// Ensure reasonable default values
			if (keySize.x        <= 0) keySize.x        = 60;
			if (keySize.y        <= 0) keySize.y        = 60;
			if (specialKeySize.x <= 0) specialKeySize.x = 120;
			if (specialKeySize.y <= 0) specialKeySize.y = 60;
		}
	}
}