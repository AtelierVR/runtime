using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Sessions;
using Nox.UI;
using UnityEngine;

namespace api.nox.session.client {
	public class SessionPage : IPage {
		internal static string GetStaticKey()
			=> "session";

		public string GetKey()
			=> GetStaticKey();

		internal int              MId;
		private  object[]         _context;
		private  GameObject       _content;
		private  SessionComponent _component;
		private  ushort           _currentId = ushort.MinValue;


		public ISession GetSession() {
			var session = Main.Instance.GetSession(_currentId);
			session ??= Main.Instance.GetCurrent();
			return session ?? Main.Instance.GetSessions().FirstOrDefault();
		}

		public void SetSession(ISession session)
			=> _currentId = session?.GetId() ?? ushort.MinValue;
		
		public void OnRefresh()
			=> Refresh(false).Forget();

		private static bool T<T>(object[] o, int index, out T value) {
			if (o.Length > index && o[index] is T t) {
				value = t;
				return true;
			}

			value = default;
			return false;
		}

		internal static IPage OnGotoAction(IMenu menu, object[] context) {
			var id = T(context, 0, out ushort cid) ? cid : ushort.MinValue;
			var page = new SessionPage {
				MId        = menu.GetId(),
				_context   = context,
				_currentId = id
			};
			page.Refresh(true).Forget();
			return page;
		}

		private async UniTask Refresh(bool load) {
			await UniTask.Yield();
			_component?.UpdateDropdown();
			UpdateLayout.UpdateImmediate(_content);
		}

		public void OnDisplay(IPage lastPage) {
			_component?.UpdateDropdown();
		}

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(MId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			(_content, _component) = SessionComponent.Generate(this, parent);
			return _content;
		}
	}
}