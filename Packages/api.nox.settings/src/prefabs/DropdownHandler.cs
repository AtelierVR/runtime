using Cysharp.Threading.Tasks;
using Nox.Settings;
using UnityEngine;

namespace Nox.CCK.Settings {
	public abstract class DropdownHandler : IHandler {
		public (string, string)[] Options;
		public int                DefaultIndex = 0;

		public abstract string[]   GetPath();

		public virtual GameObject GetContent() {
			var asset = 
		}

		public virtual UniTask<GameObject> GetContentAsync()
			=> UniTask.FromResult(GetContent());

		public virtual void OnValueChanged(string value) { }

		public
	}
}