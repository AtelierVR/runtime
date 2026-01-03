/*using System.Linq;
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

		public void SetSession(ISession session) {
			_currentId = session?.GetId() ?? ushort.MinValue;
			Refresh(false);
		}

		public void OnRefresh()
			=> Refresh(false);

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
			return new SessionPage {
				MId        = menu.GetId(),
				_context   = context,
				_currentId = id
			};
		}

		private void Refresh(bool load) {
			if (!_component) return;
			_component.UpdateTitles();
			_component.UpdateDropdown();
			_component.UpdateNavigation().Forget();
			_component.UpdateThumbnail().Forget();
		}

		public void OnDisplay(IPage lastPage)
			=> Refresh(false);

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(MId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			(_content, _component) = SessionComponent.Generate(this, parent);
			UpdateLayout.UpdateImmediate(_content);
			return _content;
		}

		public void OnOpen(IPage lastPage) {
			Main.OnCurrentChanged.AddListener(OnSessionChanged);
			Main.OnSessionAdded.AddListener(OnSessionAdded);
			Main.OnSessionRemoved.AddListener(OnSessionRemoved);
		}

		public void OnRemove() {
			Main.OnCurrentChanged.RemoveListener(OnSessionChanged);
			Main.OnSessionAdded.RemoveListener(OnSessionAdded);
			Main.OnSessionRemoved.RemoveListener(OnSessionRemoved);
		}

		private void OnSessionAdded(ISession session)
			=> Refresh(false);

		private void OnSessionRemoved(ISession session)
			=> Refresh(false);

		private void OnSessionChanged(ISession newSession, ISession oldSession)
			=> Refresh(false);
	}
}*/