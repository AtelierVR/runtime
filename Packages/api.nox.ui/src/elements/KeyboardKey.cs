using UnityEngine;
using UnityEngine.UI;
using Transform = UnityEngine.Transform;

namespace Nox.UI {
	/// <summary>
	/// Key visual states
	/// </summary>
	public enum KeyState {
		Normal,
		Highlighted,
		Pressed,
		Selected,
		Disabled
	}

	/// <summary>
	/// Basic keyboard key component that provides visual feedback and interaction
	/// Can be used as a template for custom key behaviors
	/// </summary>
	[RequireComponent(typeof(Button))]
	public class KeyboardKey : MonoBehaviour {
		[Header("Key Settings")]
		[Tooltip("The value this key represents")]
		[SerializeField] private string keyValue = "";
		
		[Tooltip("Display text (if different from key value)")]
		[SerializeField] private string displayText = "";

		[Header("Visual Feedback")]
		[Tooltip("Color when key is in normal state")]
		[SerializeField] private Color normalColor = Color.white;
		
		[Tooltip("Color when key is pressed")]
		[SerializeField] private Color pressedColor = Color.gray;
		
		[Tooltip("Color when key is highlighted")]
		[SerializeField] private Color highlightedColor = Color.lightGray;

		[Header("Animation")]
		[Tooltip("Scale when pressed")]
		[SerializeField] private float pressedScale = 0.95f;
		
		[Tooltip("Animation duration")]
		[SerializeField] private float animationDuration = 0.1f;

		[Header("Audio")]
		[Tooltip("Sound to play when key is pressed")]
		[SerializeField] private AudioClip keySound;

		// Private fields
		private Button _button;
		private Image _image;
		private Text _text;
		private TMPro.TextMeshProUGUI _tmpText;
		private AudioSource _audioSource;
		private Vector3 _originalScale;
		private Keyboard _keyboard;

		// Properties
		public string KeyValue {
			get => keyValue;
			set {
				keyValue = value;
				UpdateDisplay();
			}
		}

		public string DisplayText {
			get => string.IsNullOrEmpty(displayText) ? keyValue : displayText;
			set {
				displayText = value;
				UpdateDisplay();
			}
		}

		// Unity lifecycle
		private void Awake() {
			InitializeComponents();
			_originalScale = transform.localScale;
		}

		private void Start() {
			SetupButton();
			UpdateDisplay();
		}

		private void OnValidate() {
			if (Application.isPlaying) {
				UpdateDisplay();
			}
		}

		// Public methods
		/// <summary>
		/// Set the keyboard that owns this key
		/// </summary>
		/// <param name="keyboard">The parent keyboard</param>
		public void SetKeyboard(Keyboard keyboard) {
			_keyboard = keyboard;
		}

		/// <summary>
		/// Programmatically press this key
		/// </summary>
		public void PressKey() {
			if (_keyboard != null) {
				_keyboard.ProcessKeyPress(keyValue);
			}
			
			PlayPressAnimation();
			PlayKeySound();
		}

		/// <summary>
		/// Set the visual state of the key
		/// </summary>
		/// <param name="state">Button state</param>
		public void SetVisualState(KeyState state) {
			if (_image == null) return;

			switch (state) {
				case KeyState.Normal:
					_image.color = normalColor;
					break;
				case KeyState.Highlighted:
					_image.color = highlightedColor;
					break;
				case KeyState.Pressed:
					_image.color = pressedColor;
					break;
				case KeyState.Selected:
					_image.color = highlightedColor;
					break;
				case KeyState.Disabled:
					_image.color = Color.gray;
					break;
			}
		}

		// Private methods
		private void InitializeComponents() {
			_button = GetComponent<Button>();
			_image = GetComponent<Image>();
			_text = GetComponentInChildren<Text>();
			_tmpText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
			_audioSource = GetComponent<AudioSource>();

			// Create audio source if needed and we have a sound
			if (_audioSource == null && keySound != null) {
				_audioSource = gameObject.AddComponent<AudioSource>();
				_audioSource.playOnAwake = false;
			}
		}

		private void SetupButton() {
			if (_button == null) return;

			// Set up button colors
			var colors = _button.colors;
			colors.normalColor = normalColor;
			colors.highlightedColor = highlightedColor;
			colors.pressedColor = pressedColor;
			colors.selectedColor = highlightedColor;
			_button.colors = colors;

			// Set up button events
			_button.onClick.RemoveAllListeners();
			_button.onClick.AddListener(PressKey);
		}

		private void UpdateDisplay() {
			string textToShow = DisplayText;

			if (_text != null) {
				_text.text = textToShow;
			}

			if (_tmpText != null) {
				_tmpText.text = textToShow;
			}
		}

		private void PlayPressAnimation() {
			if (!gameObject.activeInHierarchy) return;

			// Stop any existing animation
			StopAllCoroutines();
			
			// Start press animation
			StartCoroutine(PressAnimationCoroutine());
		}

		private System.Collections.IEnumerator PressAnimationCoroutine() {
			// Scale down
			float elapsedTime = 0f;
			Vector3 startScale = transform.localScale;
			Vector3 targetScale = _originalScale * pressedScale;

			while (elapsedTime < animationDuration / 2f) {
				float t = elapsedTime / (animationDuration / 2f);
				transform.localScale = Vector3.Lerp(startScale, targetScale, t);
				elapsedTime += Time.deltaTime;
				yield return null;
			}

			transform.localScale = targetScale;

			// Scale back up
			elapsedTime = 0f;
			startScale = transform.localScale;

			while (elapsedTime < animationDuration / 2f) {
				float t = elapsedTime / (animationDuration / 2f);
				transform.localScale = Vector3.Lerp(startScale, _originalScale, t);
				elapsedTime += Time.deltaTime;
				yield return null;
			}

			transform.localScale = _originalScale;
		}

		private void PlayKeySound() {
			if (_audioSource != null && keySound != null) {
				_audioSource.PlayOneShot(keySound);
			}
		}

		// Public utility methods
		/// <summary>
		/// Create a basic key GameObject with this component
		/// </summary>
		/// <param name="keyValue">The value for this key</param>
		/// <param name="parent">Parent transform</param>
		/// <returns>Created key GameObject</returns>
		public static GameObject CreateBasicKey(string keyValue, Transform parent = null) {
			// Create the key GameObject
			GameObject keyObj = new GameObject($"Key_{keyValue}");
			if (parent != null) {
				keyObj.transform.SetParent(parent, false);
			}

			// Add RectTransform
			var rectTransform = keyObj.AddComponent<RectTransform>();
			rectTransform.sizeDelta = new Vector2(60, 60);

			// Add Image component
			keyObj.AddComponent<Image>();

			// Add Button component
			keyObj.AddComponent<Button>();

			// Add KeyboardKey component
			var keyComponent = keyObj.AddComponent<KeyboardKey>();
			keyComponent.KeyValue = keyValue;

			// Add Text component for display
			GameObject textObj = new GameObject("Text");
			textObj.transform.SetParent(keyObj.transform, false);
			
			var textRectTransform = textObj.AddComponent<RectTransform>();
			textRectTransform.anchorMin = Vector2.zero;
			textRectTransform.anchorMax = Vector2.one;
			textRectTransform.sizeDelta = Vector2.zero;
			textRectTransform.anchoredPosition = Vector2.zero;

			var text = textObj.AddComponent<Text>();
			text.text = keyValue.ToUpper();
			text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			text.color = Color.black;
			text.alignment = TextAnchor.MiddleCenter;
			text.fontSize = 14;

			return keyObj;
		}
	}
}
