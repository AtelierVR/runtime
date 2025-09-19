using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Settings;
using Nox.UI;
using UnityEngine;

namespace api.nox.settings.client {
	public class SettingsPage : IPage {
		internal static string GetStaticKey()
			=> "settings";

		public string GetKey()
			=> GetStaticKey();

		internal int               MId;
		private  object[]          _context;
		private  GameObject        _content;
		private  SettingsComponent _component;
		private  string[]          _current;

		public void OnRefresh()
			=> Refresh();

		private static bool T<T>(object[] o, int index, out T value) {
			if (o.Length > index && o[index] is T t) {
				value = t;
				return true;
			}

			value = default;
			return false;
		}

		internal static IPage OnGotoAction(IMenu menu, object[] context)
			=> new SettingsPage {
				MId      = menu.GetId(),
				_context = context,
				_current = T(context, 0, out string[] path) ? path : Array.Empty<string>()
			};


		private void Refresh() {
			if (!_component) return;
			_component.UpdateTitles();
			_component.UpdateNavigation().Forget();
			_component.UpdateContent().Forget();
		}

		public void OnDisplay(IPage lastPage)
			=> Refresh();

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(MId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			(_content, _component) = SettingsComponent.Generate(this, parent);
			UpdateLayout.UpdateImmediate(_content);
			return _content;
		}

		public void OnOpen(IPage lastPage) {
			Main.OnHandlerAdded.AddListener(OnSettingsChanged);
			Main.OnHandlerRemoved.AddListener(OnSettingsChanged);
		}

		private void OnSettingsChanged(IHandler arg0)
			=> Refresh();

		public void OnRemove() {
			Main.OnHandlerAdded.RemoveListener(OnSettingsChanged);
			Main.OnHandlerRemoved.RemoveListener(OnSettingsChanged);
		}

		public CategoryDetails GetCategory() {
			var category = _current.Length > 0 ? _current[0] : null;
			var handler  = Main.Handlers.FirstOrDefault(h => h.Split().Item1 == category);
			if (handler == null)
				category = Main.Handlers.FirstOrDefault()?.Split().Item1;
			return handler == null ? null : new CategoryDetails(category);
		}

		public CategoryDetails GetCategory(string category) {
			var handler = Main.Handlers.FirstOrDefault(h => h.Split().Item1 == category);
			return handler == null ? null : new CategoryDetails(category);
		}

		public GroupDetails[] GetGroups(string category)
			=> Main.Handlers
				.Where(h => h.Split().Item1 == category)
				.GroupBy(h => h.Split().Item2)
				.Select(
					g => new GroupDetails {
						Handlers = g.ToArray(),
						Category = category
					}
				)
				.ToArray();
	}

	public class GroupDetails {
		public IHandler[] Handlers = Array.Empty<IHandler>();
		public string     Category;
	}

	public class CategoryDetails {
		private readonly string _id;

		public CategoryDetails(string id)
			=> _id = id;

		public string GetId()
			=> _id;

		public string GetTitle()
			=> $"settings.page.{_id}.title";

		public async UniTask<Texture2D> GetIcon()
			=> await Client.GetAssetAsync<Texture2D>($"icons/{_id}.png");

		public string GetLabel()
			=> $"settings.page.{_id}.label";
	}
}