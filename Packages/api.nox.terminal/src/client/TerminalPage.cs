using Nox.UI;
using UnityEngine;

namespace api.nox.terminal.client {
	public class TerminalPage : IPage {
		internal static string GetStaticKey()
			=> "terminal";

		public string GetKey()
			=> GetStaticKey();

		private static bool T<T>(object[] o, int index, out T value) {
			if (o.Length > index && o[index] is T t) {
				value = t;
				return true;
			}

			value = default;
			return false;
		}

		internal static IPage OnGotoAction(IMenu menu, object[] context) {
			return new TerminalPage {
				_mId     = menu.GetId(),
				_context = context,
				_draft   = T(context, 0, out string draft) ? draft : null
			};
		}

		private int               _mId;
		private object[]          _context;
		private string            _draft;
		private GameObject        _content;
		private TerminalComponent _component;

		public object[] GetContext()
			=> _context;

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			(_content, _component) = TerminalComponent.Generate(this, parent);
			return _content;
		}

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(_mId);
	}
}