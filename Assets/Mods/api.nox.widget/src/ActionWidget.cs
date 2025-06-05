using System;
using System.Collections.Generic;
using Nox.Widgets;
using UnityEngine;

namespace api.nox.widget {
	public class ActionWidget : IWidget {
		private string           _key;
		private Vector2Int       _size;
		private Func<GameObject> _actionGetContent;

		public string GetKey()
			=> _key;

		public Vector2Int GetSize()
			=> _size;

		public GameObject GetContent()
			=> _actionGetContent?.Invoke();

		internal static ActionWidget From(Dictionary<string, object> data) {
			if (data == null || data.Count == 0)
				return null;

			var widget = new ActionWidget();

			if (data.TryGetValue("key", out var k0) && k0 is string k1)
				widget._key = k1;
			else return null;
			if (data.TryGetValue("content", out var c0) && c0 is Func<GameObject> c1)
				widget._actionGetContent = c1;
			else return null;
			
			if (data.TryGetValue("size", out var s0) && s0 is Vector2Int s1)
				widget._size  = s1;
			else widget._size = Vector2Int.one;

			return widget;
		}
	}
}