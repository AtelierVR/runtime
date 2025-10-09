using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.Users;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace api.nox.user.client {
	public class UserPage : IPage {
		internal static string GetStaticKey()
			=> "user";

		public string GetKey()
			=> GetStaticKey();

		private int            _mId;
		private object[]       _context;
		private GameObject     _content;
		private UserComponent  _component;
		private IUserIdentifier _identifier;
		private IUser           _user;
		private bool           _isLoading;


		public void OnRefresh()
			=> Refresh().Forget();

		private static bool T<T>(object[] o, int index, out T value) {
			if (o.Length > index && o[index] is T t) {
				value = t;
				return true;
			}

			value = default;
			return false;
		}

		internal static IPage OnGotoAction(IMenu menu, object[] context) {
			if (!T(context, 0, out string type)) return null;
			switch (type) {
				case "id_server" when T(context, 1, out uint id0) && T(context, 2, out string ser0):
					return OnPageByIdentifier(menu, context, new UserIdentifier(id0, ser0));
				case "identifier" when T(context, 1, out string id2):
					return OnPageByIdentifier(menu, context, UserIdentifier.FromString(id2));
				case "identifier" when T(context, 1, out IUserIdentifier ui0):
					return OnPageByIdentifier(menu, context, UserIdentifier.FromBase(ui0));
				case "user" when T(context, 1, out IUser usr3):
					return OnPageByUser(menu, context, usr3);
			}

			return null;
		}

		private static UserPage OnPageByIdentifier(IMenu menu, object[] context, UserIdentifier identifier) {
			var page = new UserPage {
				_mId        = menu.GetId(),
				_context    = context,
				_identifier = identifier,
				_user       = null
			};
			page.Refresh().Forget();
			return page;
		}

		private static UserPage OnPageByUser(IMenu menu, object[] context, IUser user) {
			return new UserPage {
				_mId        = menu.GetId(),
				_context    = context,
				_identifier = user.ToIdentifier(),
				_user       = user
			};
		}


		private async UniTask Refresh() {
			if (_isLoading) return;
			_isLoading = true;
			await UniTask.Yield();
			_isLoading = false;
			UpdateLayout.UpdateImmediate(_content);
		}

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(_mId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			(_content, _component) = UserComponent.Generate(this, parent);
			_component.UpdateLoading();
			return _content;
		}

		public void OnDisplay(IPage lastPage) {
			if (_user != null) _component.UpdateContent(_user);
			else if (_isLoading) _component.UpdateLoading();
			else _component.UpdateError("User not found or loading failed.");
		}
	}
}