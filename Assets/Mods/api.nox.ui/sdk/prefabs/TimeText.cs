using Nox.CCK.Language;
using UnityEngine;

namespace Nox.UI.Prefabs {
	#if UNITY_EDITOR
	[ExecuteAlways]
	#endif
	public class TimeText : MonoBehaviour {
		private TextLanguage text
			=> GetComponent<TextLanguage>();

		void Update()
			=> text.UpdateText(
				new[] {
					// hours 12
					System.DateTime.Now.ToString("hh"),
					// hours 24
					System.DateTime.Now.ToString("HH"),
					// minutes
					System.DateTime.Now.ToString("mm"),
					// seconds
					System.DateTime.Now.ToString("ss"),
					// ms
					System.DateTime.Now.ToString("fff"),
					// am-pm
					System.DateTime.Now.ToString("tt"),
					// day
					System.DateTime.Now.ToString("dd"),
					// month
					System.DateTime.Now.ToString("MM"),
					// year
					System.DateTime.Now.ToString("yyyy")
				}
			);
	}
}