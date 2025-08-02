using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nox.UI {
	[ExecuteAlways]
	[RequireComponent(typeof(RectTransform))]
	[DisallowMultipleComponent]
	public class KeepSize : UIBehaviour, ILayoutSelfController {
		public void SetLayoutHorizontal() { }
		public void SetLayoutVertical()   { }

		private RectTransform _mRect;
		private Vector2       _last = Vector2.zero;

		private RectTransform RectTransform {
			get {
				if (!_mRect)
					_mRect = GetComponent<RectTransform>();
				return _mRect;
			}
		}

		protected override void OnEnable() {
			base.OnEnable();
			UpdateRect();
		}

		private Vector2 GetParentSize() {
			var parent = RectTransform.parent as RectTransform;
			return !parent ? Vector2.zero : parent.rect.size;
		}


		void Update() {
			if (!Application.isPlaying) {
				UpdateRect();
				return;
			}

			var crt = GetParentSize();
			if (crt == _last) return;
			_last = crt;
			UpdateRect();
		}


		private void UpdateRect() {
			var parentSize = GetParentSize();
			var size       = RectTransform.sizeDelta;
			if (parentSize == Vector2.zero) return;

			var widthScale  = parentSize.x / size.x;
			var heightScale = parentSize.y / size.y;
			
			RectTransform.localScale = new Vector3(
				widthScale,
				heightScale,
				Mathf.Approximately(widthScale, heightScale) ? widthScale : 1f
			);
		}
	}
}