using Cysharp.Threading.Tasks;
using Nox.UI;
using UnityEngine;

namespace api.nox.ui.defaults {
	public class ExamplePage : IPage {
		public static string GetStaticKey()
			=> "example";

		private readonly int        _mId;
		private readonly object[]   _context;
		private          GameObject _content;

		public static IPage OnGotoAction(IMenu menu, object[] o)
			=> new ExamplePage(menu.GetId(), o);

		private ExamplePage(int mId, object[] context) {
			_mId     = mId;
			_context = context;
		}

		public string GetKey()
			=> GetStaticKey();

		public object[] GetContext()
			=> _context;

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			return null;
		}

		public UniTask<GameObject> GetContentAsync(RectTransform parent)
			=> UniTask.FromResult(GetContent(parent));

		public IMenu GetMenu()
			=> Client.Instance.Get<IMenu>(_mId);

		public override string ToString()
			=> $"{GetType().Name}[Key={GetKey()}, MenuId={_mId}, Context=[{string.Join(", ", _context)}]]";
	}
}