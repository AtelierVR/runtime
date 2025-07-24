using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Nox.ModLoader;

namespace DefaultNamespace {
	public class PerformanceGraph : MonoBehaviour {
		public int graphWidth  = 300;
		public int graphHeight = 100;
		public int maxSamples  = 300;

		private Dictionary<string, List<float>> samples = new();

		void Update() {
			foreach (var mod in ModManager.Mods) {
				string modName = mod.GetMetadata().GetId();
				var perf = mod.GetPerformances()
					.Where(p => p.GetName() == "update")
					.OrderByDescending(p => p.End)
					.FirstOrDefault();

				if (perf == null)
					continue;

				float ms = (float)perf.Duration.TotalMilliseconds;

				if (!samples.ContainsKey(modName))
					samples[modName] = new List<float>();

				var list = samples[modName];
				if (list.Count >= maxSamples)
					list.RemoveAt(0);
				list.Add(ms);
			}
		}

		void OnGUI() {
			int yOffset = 10;
			foreach (var kv in samples) {
				string      modName = kv.Key;
				List<float> data    = kv.Value;

				GUI.color = Color.white;
				GUI.Label(new Rect(10, yOffset, 200, 20), $"{modName} (last: {data.Last():0.00} ms)");

				GUI.color = Color.gray;
				GUI.DrawTexture(new Rect(10, yOffset + 20, graphWidth, graphHeight), Texture2D.whiteTexture);

				GUI.color = Color.cyan;
				for (int i = 1; i < data.Count; i++) {
					float x1 = 10                         + (graphWidth * (i - 1)      / (float)maxSamples);
					float y1 = yOffset + 20 + graphHeight - Mathf.Min(data[i - 1], 33) * graphHeight / 33f;

					float x2 = 10                         + (graphWidth * i        / (float)maxSamples);
					float y2 = yOffset + 20 + graphHeight - Mathf.Min(data[i], 33) * graphHeight / 33f;

					Drawing.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), Color.cyan, 1f);
				}

				yOffset += graphHeight + 40;
			}
		}
	}
}