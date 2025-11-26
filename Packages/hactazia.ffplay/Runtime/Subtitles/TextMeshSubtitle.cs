using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Hactazia.FFPlay {
	[RequireComponent(typeof(TextMeshProUGUI))]
	public class TextMeshSubtitle : MonoBehaviour {
		public SubtitleWorker worker;

		private TextMeshProUGUI _text;

		private void Start() {
			worker ??= GetComponentInParent<SubtitleWorker>(true);

			if (!worker) {
				Debug.LogWarning($"No {nameof(SubtitleWorker)} found.");
				return;
			}

			_text = GetComponent<TextMeshProUGUI>();
			worker.onDisplay.AddListener(OnDisplay);
			worker.onClear.AddListener(OnClear);
		}

		private void OnDestroy() {
			worker?.onDisplay.RemoveListener(OnDisplay);
			worker?.onClear.RemoveListener(OnClear);
		}

		private void OnDisplay(string text) {
			if (!_text) return;
			_text.text = text;
		}

		private void OnClear(string _) {
			if (!_text) return;
			_text.text = string.Empty;
		}
	}
}