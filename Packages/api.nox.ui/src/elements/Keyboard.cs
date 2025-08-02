using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Nox.CCK.Utils;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace Nox.UI {
	/// <summary>
	/// A flexible keyboard MonoBehaviour that can be configured with different layouts
	/// Supports various input modes and customizable key behaviors
	/// </summary>
	[DisallowMultipleComponent]
	public class Keyboard : MonoBehaviour {
		[Header("Layout Configuration")]
		[Tooltip("The layout component that defines the keyboard structure")]
		[SerializeField] private MonoBehaviour layoutComponent;
		
		[Header("Keyboard Settings")]
		[Tooltip("Input mode: text, numeric, password, etc.")]
		[SerializeField] private string inputMode = "text";
		
		[Tooltip("Should the keyboard be case sensitive")]
		[SerializeField] private bool caseSensitive = false;
		
		[Tooltip("Auto-hide keyboard after input")]
		[SerializeField] private bool autoHide = false;
		
		[Tooltip("Maximum input length (0 = unlimited)")]
		[SerializeField] private int maxInputLength = 0;

		[Header("Visual Settings")]
		[Tooltip("Container where keys will be instantiated")]
		[SerializeField] private Transform keyContainer;
		
		[Tooltip("Default key prefab (optional)")]
		[SerializeField] private GameObject defaultKeyPrefab;
		
		[Tooltip("Key spacing")]
		[SerializeField] private float keySpacing = 5f;
		
		[Tooltip("Keyboard background (optional)")]
		[SerializeField] private Image background;

		[Header("Audio Settings")]
		[Tooltip("Sound to play on key press")]
		[SerializeField] private AudioClip keyPressSound;
		
		[Tooltip("Audio source for keyboard sounds")]
		[SerializeField] private AudioSource audioSource;

		[Header("Events")]
		[Tooltip("Event fired when a key is pressed")]
		public UnityEvent<string> OnKeyPressed = new UnityEvent<string>();
		
		[Tooltip("Event fired when a key is released")]
		public UnityEvent<string> OnKeyReleased = new UnityEvent<string>();
		
		[Tooltip("Event fired when text input changes")]
		public UnityEvent<string> OnTextChanged = new UnityEvent<string>();
		
		[Tooltip("Event fired when Enter/Return is pressed")]
		public UnityEvent<string> OnSubmit = new UnityEvent<string>();
		
		[Tooltip("Event fired when keyboard is shown")]
		public UnityEvent OnKeyboardShown = new UnityEvent();
		
		[Tooltip("Event fired when keyboard is hidden")]
		public UnityEvent OnKeyboardHidden = new UnityEvent();

		// Private fields
		private IKeyboardLayout _currentLayout;
		private string _currentText = "";
		private bool _isShiftPressed = false;
		private bool _isCapsLockOn = false;
		private bool _isVisible = true;
		private readonly List<GameObject> _instantiatedKeys = new List<GameObject>();

		// Properties
		/// <summary>
		/// Get or set the current layout
		/// </summary>
		public IKeyboardLayout CurrentLayout {
			get => _currentLayout;
			set => SetLayout(value);
		}

		/// <summary>
		/// Get or set the current input text
		/// </summary>
		public string CurrentText {
			get => _currentText;
			set => SetText(value);
		}

		/// <summary>
		/// Get or set the input mode
		/// </summary>
		public string InputMode {
			get => inputMode;
			set => SetInputMode(value);
		}

		/// <summary>
		/// Check if the keyboard is currently visible
		/// </summary>
		public bool IsVisible => _isVisible;

		/// <summary>
		/// Get or set case sensitivity
		/// </summary>
		public bool CaseSensitive {
			get => caseSensitive;
			set => caseSensitive = value;
		}

		/// <summary>
		/// Get or set maximum input length
		/// </summary>
		public int MaxInputLength {
			get => maxInputLength;
			set => maxInputLength = Mathf.Max(0, value);
		}

		// Unity lifecycle
		private void Start() {
			InitializeKeyboard();
		}

		private void OnValidate() {
			// Ensure we have a key container
			if (keyContainer == null) {
				keyContainer = transform;
			}

			// Validate layout component
			if (layoutComponent != null && !(layoutComponent is IKeyboardLayout)) {
				Logger.LogWarning($"Layout component {layoutComponent.name} does not implement IKeyboardLayout interface");
				layoutComponent = null;
			}
		}

		// Inspector Setup Methods
		[ContextMenu("Setup Keyboard")]
		public void SetupKeyboard() {
			InitializeKeyboard();
		}

		/// <summary>
		/// Setup keyboard components automatically
		/// </summary>
		public void SetupKeyboardComponents() {
			try {
				Logger.LogDebug("Setting up keyboard components...");

				// Setup key container if not assigned
				if (keyContainer == null) {
					// Create a key container GameObject
					GameObject containerObj = new GameObject("KeyContainer");
					containerObj.transform.SetParent(transform, false);
					
					// Add RectTransform for UI layout
					var rectTransform = containerObj.AddComponent<RectTransform>();
					rectTransform.anchorMin = Vector2.zero;
					rectTransform.anchorMax = Vector2.one;
					rectTransform.sizeDelta = Vector2.zero;
					rectTransform.anchoredPosition = Vector2.zero;
					
					// Add layout component for automatic key arrangement
					var layoutGroup = containerObj.AddComponent<GridLayoutGroup>();
					layoutGroup.cellSize = new Vector2(60, 60);
					layoutGroup.spacing = new Vector2(keySpacing, keySpacing);
					layoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
					layoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
					layoutGroup.childAlignment = TextAnchor.MiddleCenter;
					
					keyContainer = containerObj.transform;
					Logger.LogDebug("Created key container with GridLayoutGroup");
				}

				// Setup audio source if not assigned and we have a sound
				if (audioSource == null && keyPressSound != null) {
					audioSource = GetComponent<AudioSource>();
					if (audioSource == null) {
						audioSource = gameObject.AddComponent<AudioSource>();
						audioSource.playOnAwake = false;
						audioSource.volume = 0.5f;
						Logger.LogDebug("Created audio source component");
					}
				}

				// Setup background if not assigned
				if (background == null) {
					background = GetComponent<Image>();
					if (background == null) {
						background = gameObject.AddComponent<Image>();
						background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
						Logger.LogDebug("Created background image component");
					}
				}

				// Ensure we have a RectTransform for UI positioning
				if (GetComponent<RectTransform>() == null) {
					gameObject.AddComponent<RectTransform>();
					Logger.LogDebug("Added RectTransform component");
				}

				// Setup Canvas components if we're in a Canvas hierarchy
				Canvas parentCanvas = GetComponentInParent<Canvas>();
				if (parentCanvas == null) {
					Logger.LogWarning("Keyboard is not in a Canvas hierarchy. Consider placing it under a Canvas for proper UI rendering.");
				}

				// Create default layout if none assigned
				if (layoutComponent == null) {
					SetupDefaultLayout();
				}

				Logger.LogDebug("Keyboard setup completed successfully!");

#if UNITY_EDITOR
				// Mark the object as dirty for Unity Editor
				UnityEditor.EditorUtility.SetDirty(this);
#endif

			} catch (Exception e) {
				Logger.LogError($"Failed to setup keyboard components: {e.Message}");
			}
		}

		/// <summary>
		/// Setup a default QWERTY layout if none is assigned
		/// </summary>
		private void SetupDefaultLayout() {
			try {
				// Look for existing layout components
				var existingLayout = GetComponent<IKeyboardLayout>();
				if (existingLayout != null) {
					layoutComponent = existingLayout as MonoBehaviour;
					Logger.LogDebug("Found existing layout component");
					return;
				}

				// Try to create a basic layout component
				// Note: This would require having a default layout class available
				// For now, just log that a layout needs to be assigned
				Logger.LogWarning("No layout component found. Please assign a layout component that implements IKeyboardLayout interface.");
				
			} catch (Exception e) {
				Logger.LogError($"Failed to setup default layout: {e.Message}");
			}
		}

		// Public methods
		/// <summary>
		/// Initialize the keyboard with the current settings
		/// </summary>
		public void InitializeKeyboard() {
			try {
				// Clear any existing keys
				ClearKeys();

				// Set up audio source if needed
				if (audioSource == null && keyPressSound != null) {
					audioSource = GetComponent<AudioSource>();
					if (audioSource == null) {
						audioSource = gameObject.AddComponent<AudioSource>();
					}
				}

				// Initialize layout
				if (layoutComponent is IKeyboardLayout layout) {
					SetLayout(layout);
				} else {
					Logger.LogWarning("No valid keyboard layout assigned");
				}

				Logger.LogDebug($"Keyboard initialized with input mode: {inputMode}");
			} catch (Exception e) {
				Logger.LogError($"Failed to initialize keyboard: {e.Message}");
			}
		}

		/// <summary>
		/// Set a new layout for the keyboard
		/// </summary>
		/// <param name="layout">The new layout to use</param>
		public void SetLayout(IKeyboardLayout layout) {
			try {
				// Cleanup old layout
				_currentLayout?.Cleanup();

				// Clear existing keys
				ClearKeys();

				// Set new layout
				_currentLayout = layout;

				if (_currentLayout != null) {
					// Check if layout supports current input mode
					if (!_currentLayout.SupportsInputMode(inputMode)) {
						Logger.LogWarning($"Layout {_currentLayout.GetLayoutName()} does not support input mode: {inputMode}");
					}

					// Initialize and create keys
					_currentLayout.Initialize(this);
					_currentLayout.CreateKeys(keyContainer);

					Logger.LogDebug($"Layout changed to: {_currentLayout.GetLayoutName()}");
				}
			} catch (Exception e) {
				Logger.LogError($"Failed to set keyboard layout: {e.Message}");
			}
		}

		/// <summary>
		/// Set the input mode and refresh layout if needed
		/// </summary>
		/// <param name="mode">New input mode</param>
		public void SetInputMode(string mode) {
			if (inputMode == mode) return;

			inputMode = mode;
			
			// Refresh layout if it doesn't support the new mode
			if (_currentLayout != null && !_currentLayout.SupportsInputMode(mode)) {
				Logger.LogDebug($"Current layout doesn't support mode {mode}, keeping current layout");
			}

			Logger.LogDebug($"Input mode changed to: {mode}");
		}

		/// <summary>
		/// Show the keyboard
		/// </summary>
		public void Show() {
			if (_isVisible) return;

			_isVisible = true;
			gameObject.SetActive(true);
			OnKeyboardShown.Invoke();
			
			Logger.LogDebug("Keyboard shown");
		}

		/// <summary>
		/// Hide the keyboard
		/// </summary>
		public void Hide() {
			if (!_isVisible) return;

			_isVisible = false;
			gameObject.SetActive(false);
			OnKeyboardHidden.Invoke();
			
			Logger.LogDebug("Keyboard hidden");
		}

		/// <summary>
		/// Toggle keyboard visibility
		/// </summary>
		public void Toggle() {
			if (_isVisible) {
				Hide();
			} else {
				Show();
			}
		}

		/// <summary>
		/// Process a key press
		/// </summary>
		/// <param name="key">The key that was pressed</param>
		public void ProcessKeyPress(string key) {
			try {
				// Handle special keys
				switch (key.ToLower()) {
					case "backspace":
						HandleBackspace();
						break;
					case "enter":
					case "return":
						HandleSubmit();
						break;
					case "space":
						HandleSpace();
						break;
					case "shift":
						HandleShift();
						break;
					case "capslock":
						HandleCapsLock();
						break;
					case "tab":
						HandleTab();
						break;
					default:
						HandleCharacterInput(key);
						break;
				}

				// Play sound
				PlayKeySound();

				// Fire events
				OnKeyPressed.Invoke(key);
				_currentLayout?.OnKeyPressed(key);

				Logger.LogDebug($"Key pressed: {key}");
			} catch (Exception e) {
				Logger.LogError($"Error processing key press '{key}': {e.Message}");
			}
		}

		/// <summary>
		/// Process a key release
		/// </summary>
		/// <param name="key">The key that was released</param>
		public void ProcessKeyRelease(string key) {
			try {
				OnKeyReleased.Invoke(key);
				_currentLayout?.OnKeyReleased(key);

				Logger.LogDebug($"Key released: {key}");
			} catch (Exception e) {
				Logger.LogError($"Error processing key release '{key}': {e.Message}");
			}
		}

		/// <summary>
		/// Set the current text
		/// </summary>
		/// <param name="text">New text value</param>
		public void SetText(string text) {
			var newText = text ?? "";
			
			// Apply length limit
			if (maxInputLength > 0 && newText.Length > maxInputLength) {
				newText = newText.Substring(0, maxInputLength);
			}

			_currentText = newText;
			OnTextChanged.Invoke(_currentText);
		}

		/// <summary>
		/// Clear the current text
		/// </summary>
		public void ClearText() {
			SetText("");
		}

		/// <summary>
		/// Add text to the current input
		/// </summary>
		/// <param name="text">Text to add</param>
		public void AppendText(string text) {
			SetText(_currentText + text);
		}

		// Private helper methods
		private void HandleBackspace() {
			if (_currentText.Length > 0) {
				SetText(_currentText.Substring(0, _currentText.Length - 1));
			}
		}

		private void HandleSubmit() {
			OnSubmit.Invoke(_currentText);
			
			if (autoHide) {
				Hide();
			}
		}

		private void HandleSpace() {
			HandleCharacterInput(" ");
		}

		private void HandleShift() {
			_isShiftPressed = !_isShiftPressed;
			// TODO: Update key displays to show shifted characters
		}

		private void HandleCapsLock() {
			_isCapsLockOn = !_isCapsLockOn;
			// TODO: Update key displays to show caps lock state
		}

		private void HandleTab() {
			HandleCharacterInput("\t");
		}

		private void HandleCharacterInput(string character) {
			if (string.IsNullOrEmpty(character)) return;

			// Apply case modifications
			var finalChar = character;
			if (_isShiftPressed || _isCapsLockOn) {
				finalChar = character.ToUpper();
			} else if (!caseSensitive) {
				finalChar = character.ToLower();
			}

			// Reset shift after use (but not caps lock)
			if (_isShiftPressed) {
				_isShiftPressed = false;
			}

			AppendText(finalChar);
		}

		private void PlayKeySound() {
			if (audioSource != null && keyPressSound != null) {
				audioSource.PlayOneShot(keyPressSound);
			}
		}

		private void ClearKeys() {
			foreach (var key in _instantiatedKeys) {
				if (key != null) {
					DestroyImmediate(key);
				}
			}
			_instantiatedKeys.Clear();
		}

		// Public utility methods for layouts
		/// <summary>
		/// Register a key GameObject (for cleanup tracking)
		/// </summary>
		/// <param name="keyObject">The key GameObject to register</param>
		public void RegisterKey(GameObject keyObject) {
			if (keyObject != null && !_instantiatedKeys.Contains(keyObject)) {
				_instantiatedKeys.Add(keyObject);
			}
		}

		/// <summary>
		/// Get the default key prefab
		/// </summary>
		/// <returns>Default key prefab or null if not set</returns>
		public GameObject GetDefaultKeyPrefab() {
			return defaultKeyPrefab;
		}

		/// <summary>
		/// Get the key spacing value
		/// </summary>
		/// <returns>Key spacing in pixels</returns>
		public float GetKeySpacing() {
			return keySpacing;
		}

		/// <summary>
		/// Get the key container transform
		/// </summary>
		/// <returns>Transform where keys should be placed</returns>
		public Transform GetKeyContainer() {
			return keyContainer;
		}
	}
}
