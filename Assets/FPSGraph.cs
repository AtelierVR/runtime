using System.Collections.Generic;
using System.Linq;
using Nox.ModLoader;
using UnityEngine;

namespace DefaultNamespace {
	public class FPSGraph : MonoBehaviour {
		public int graphWidth  = 200;
		public int graphHeight = 100;
		public int maxSamples  = 200;

		private List<float> fpsSamples = new List<float>();
		private float       deltaTime;

		void Update() {
			deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
			float fps = 1.0f / deltaTime;

			if (fpsSamples.Count >= maxSamples)
				fpsSamples.RemoveAt(0);
			
			fpsSamples.Add(fps);
		}

		void OnGUI() {
			GUI.color = Color.black;
			GUI.DrawTexture(new Rect(10, 10, graphWidth, graphHeight), Texture2D.whiteTexture);

			GUI.color = Color.green;
			for (int i = 1; i < fpsSamples.Count; i++) {
				float x1 = 10               + (graphWidth * (i - 1)                  / (float)maxSamples);
				float y1 = 10 + graphHeight - Mathf.Clamp(fpsSamples[i - 1], 0, 100) * graphHeight / 100f;

				float x2 = 10               + (graphWidth * i                    / (float)maxSamples);
				float y2 = 10 + graphHeight - Mathf.Clamp(fpsSamples[i], 0, 100) * graphHeight / 100f;

				Drawing.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), Color.green, 1f);
			}

			GUI.color = Color.white;
			GUI.Label(new Rect(10, 10, 100, 20), $"FPS: {(1.0f / deltaTime):0.0}");
		}
	}
}