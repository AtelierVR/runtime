using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace {
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.UI;

	[RequireComponent(typeof(CanvasRenderer))]
	public class UGUIFPSGraph : Graphic {
		public int   maxSamples  = 200;
		public float graphHeight = 100f;

		private List<float> fpsSamples = new List<float>();
		private float       deltaTime;

		protected override void OnPopulateMesh(VertexHelper vh) {
			vh.Clear();

			if (fpsSamples.Count < 2)
				return;

			float widthPerSample = rectTransform.rect.width / (float)(maxSamples - 1);
			float height         = rectTransform.rect.height;

			for (int i = 1; i < fpsSamples.Count; i++) {
				float x1 = (i - 1)                                 * widthPerSample;
				float y1 = Mathf.Clamp01(fpsSamples[i - 1] / 100f) * graphHeight;

				float x2 = i                                   * widthPerSample;
				float y2 = Mathf.Clamp01(fpsSamples[i] / 100f) * graphHeight;

				AddLine(vh, new Vector2(x1, y1), new Vector2(x2, y2), 1.5f, color);
			}
		}

		void Update() {
			deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
			float fps = 1f / deltaTime;

			if (fpsSamples.Count >= maxSamples)
				fpsSamples.RemoveAt(0);

			fpsSamples.Add(fps);
			SetVerticesDirty();
		}

		private void AddLine(VertexHelper vh, Vector2 start, Vector2 end, float thickness, Color col) {
			Vector2 dir    = (end - start).normalized;
			Vector2 normal = new Vector2(-dir.y, dir.x) * thickness * 0.5f;

			UIVertex[] quad = new UIVertex[4];

			quad[0].position = start - normal;
			quad[1].position = start + normal;
			quad[2].position = end   + normal;
			quad[3].position = end   - normal;

			for (int i = 0; i < 4; i++) {
				quad[i].color = col;
			}

			int idx = vh.currentVertCount;
			vh.AddUIVertexQuad(quad);
		}
	}

}