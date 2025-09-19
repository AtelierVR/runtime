using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Settingss;
using Nox.UI;
using UnityEngine;

namespace api.nox.settings.client {
	public class SettingPage : IPage {
		internal static string GetStaticKey()
			=> "settings";

		public string GetKey()
			=> GetStaticKey();

		internal int              MId;
		private  object[]         _context;
		private  GameObject       _content;
		private  SettingComponent _component;
		private  ushort           _currentId = ushort.MinValue;


		public ISettings GetSettings() {
			var settings = Main.Instance.GetSettings(_currentId);
			settings ??= Main.Instance.GetCurrent();
			return settings ?? Main.Instance.GetSettingss().FirstOrDefault();
		}

		public void SetSettings(ISettings settings) {
			_currentId = settings?.GetId() ?? ushort.MinValue;
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
			return new SettingPage {
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
			(_content, _component) = SettingComponent.Generate(this, parent);
			UpdateLayout.UpdateImmediate(_content);
			return _content;
		}

		public void OnOpen(IPage lastPage) {
			Main.OnCurrentChanged.AddListener(OnSettingsChanged);
			Main.OnSettingsAdded.AddListener(OnSettingsAdded);
			Main.OnSettingsRemoved.AddListener(OnSettingsRemoved);
		}

		public void OnRemove() {
			Main.OnCurrentChanged.RemoveListener(OnSettingsChanged);
			Main.OnSettingsAdded.RemoveListener(OnSettingsAdded);
			Main.OnSettingsRemoved.RemoveListener(OnSettingsRemoved);
		}

		private void OnSettingsAdded(ISettings settings)
			=> Refresh(false);

		private void OnSettingsRemoved(ISettings settings)
			=> Refresh(false);

		private void OnSettingsChanged(ISettings newSettings, ISettings oldSettings)
			=> Refresh(false);
	}
}