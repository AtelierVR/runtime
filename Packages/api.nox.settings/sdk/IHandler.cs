using System;
using Cysharp.Threading.Tasks;
using Nox.UI;
using UnityEngine;

namespace Nox.Settings {
	public interface IHandler : IComparable<IHandler> {
		public string[] GetPath();

		public bool IsActive();

		public GameObject GetContent(RectTransform transform, IMenu menu);

		public UniTask<GameObject> GetContentAsync(RectTransform transform, IMenu menu);

		public void OnUpdated(IHandler handler);
	}
}