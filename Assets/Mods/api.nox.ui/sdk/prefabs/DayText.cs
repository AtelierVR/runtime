using Nox.CCK.Language;
using UnityEngine;

namespace api.nox.game {
	public class DayText : MonoBehaviour {
		public string textKey = "day.{0}";

		void Update()
			=> GetComponent<TextLanguage>()
				.UpdateText(string.Format(textKey, System.DateTime.Now.DayOfWeek.ToString().ToLower()));
	}
}