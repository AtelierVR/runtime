using System;
using UnityEngine;

namespace Nox.UI {
	[Serializable]
	public class NavigationData {
		public string                      key;
		public Func<Transform, GameObject> getCustomContent;
		public Texture2D                   icon;
		public string                      iconPath;
		public string                      text;
		public string[]                    textArguments;
		public string                      tooltip;
		public string[]                    tooltipArguments;
		public NavigationExecution         executionType;
		public string                      execution;
		public object[]                    executionArguments;
		public NavigationFlags             flags;
	}
}