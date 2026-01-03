using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI {
	[Serializable]
	public class NavigationData {
		public string                               key;
		public Func<Transform, UniTask<GameObject>> GetCustomContent;
		public Texture2D                            icon;
		public ResourceIdentifier                   iconPath;
		public string                               text;
		public string[]                             textArguments;
		public string                               tooltip;
		public string[]                             tooltipArguments;
		public NavigationExecution                  executionType;
		public string                               execution;
		public object[]                             ExecutionArguments;
		public NavigationFlags                      flags;
	}
}