using System;
using System.Collections.Generic;
using Nox.CCK.Utils;
using Nox.UI;
using UnityEngine;
using Transform = UnityEngine.Transform;

namespace api.nox.ui.pages {
	public class ActionPage : IPage, INoxObject {
		private string           _key;
		private object[]         _context;
		private GameObject       _content;
		private Func<RectTransform, GameObject> _actionGetContent;
		private Action<IPage>    _actionOnOpen;
		private Action<IPage>    _actionOnRestore;
		private Action           _actionOnRemove;
		private Action<IPage>    _actionOnDisplay;
		private Action<IPage>    _actionOnHide;

		internal static ActionPage From(Dictionary<string, object> data) {
			if (data == null || data.Count == 0)
				return null;

			var page = new ActionPage();

			if (data.TryGetValue("key", out var k0) && k0 is string k1)
				page._key = k1;
			else return null;

			if (data.TryGetValue("content", out var g0) && g0 is Func<RectTransform, GameObject> g1)
				page._actionGetContent = g1;
			else return null;

			if (data.TryGetValue("open", out var o0) && o0 is Action<IPage> o1)
				page._actionOnOpen = o1;

			if (data.TryGetValue("restore", out var r0) && r0 is Action<IPage> r1)
				page._actionOnRestore = r1;

			if (data.TryGetValue("remove", out var m0) && m0 is Action m1)
				page._actionOnRemove = m1;

			if (data.TryGetValue("display", out var d0) && d0 is Action<IPage> d1)
				page._actionOnDisplay = d1;

			if (data.TryGetValue("hide", out var h0) && h0 is Action<IPage> h1)
				page._actionOnHide = h1;

			return page;
		}

		public string GetKey()
			=> _key;

		public object[] GetContext()
			=> _context ??= Array.Empty<object>();


		public bool TryGetContext<T>(out T context) {
			context = default;
			if (_context == null || _context.Length == 0)
				return false;

			foreach (var obj in _context) {
				if (obj is not T t) continue;
				context = t;
				return true;
			}

			return false;
		}

		public GameObject GetContent(RectTransform parent)
			=> _content ??= _actionGetContent?.Invoke(parent) ?? throw new InvalidOperationException("ActionPage content is not set or actionGetContent is null.");

		public IMenu GetMenu() {
			throw new NotImplementedException();
		}

		public void OnOpen(IPage lastPage)
			=> _actionOnOpen?.Invoke(lastPage);

		public void OnRestore(IPage lastPage)
			=> _actionOnRestore?.Invoke(lastPage);

		public void OnDisplay(IPage lastPage)
			=> _actionOnDisplay?.Invoke(lastPage);

		public void OnHide(IPage nextPage)
			=> _actionOnHide?.Invoke(nextPage);

		public void OnRemove()
			=> _actionOnRemove?.Invoke();
	}
}