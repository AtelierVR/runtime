using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI {
	[Serializable]
	public class NavigationData {
		public string                               Key;
		public Func<Transform, UniTask<GameObject>> GetCustomContent;
		public Texture2D                            icon;
		public ResourceIdentifier                   Icon;
		public string                               text;
		public string[]                             textArguments;
		public string                               tooltip;
		public string[]                             tooltipArguments;
		public NavigationExecution                  executionType;
		public string                               Action;
		public object[]                             ExecutionArguments;
		public NavigationFlags                      Flags;
	}
}