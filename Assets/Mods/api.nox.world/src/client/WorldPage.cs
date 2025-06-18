using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Search;
using Nox.UI;
using Nox.Worlds;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.world.client {
	public class WorldPage : IPage {
		internal static string GetStaticKey()
			=> "world";

		public string GetKey()
			=> GetStaticKey();

		private int              _mId;
		private object[]         _context;
		private GameObject       _content;
		private WorldComponent   _component;
		private IWorldIdentifier _identifier;
		private IWorld           _world;
		private bool             _isLoading;


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
					return OnPageByIdentifier(menu, context, new WorldIdentifier(id0, null, ser0));
				case "identifier" when T(context, 1, out string id2):
					return OnPageByIdentifier(menu, context, WorldIdentifier.FromString(id2));
				case "world" when T(context, 1, out World usr3):
					return OnPageByWorld(menu, context, usr3);
			}

			return null;
		}

		private static WorldPage OnPageByIdentifier(IMenu menu, object[] context, WorldIdentifier identifier) {
			var page = new WorldPage {
				_mId        = menu.GetId(),
				_context    = context,
				_identifier = identifier,
				_world      = null
			};
			page.Refresh().Forget();
			return page;
		}

		private static WorldPage OnPageByWorld(IMenu menu, object[] context, World world) {
			return new WorldPage {
				_mId        = menu.GetId(),
				_context    = context,
				_identifier = world.ToInternalIdentifier(),
				_world      = world
			};
		}


		private async UniTask Refresh() {
			if (_isLoading) return;
			_isLoading = true;
			await UniTask.Yield();
			_isLoading = false;
		}

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(_mId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			(_content, _component) = WorldComponent.Generate(this, parent);
			_component.UpdateLoading();
			return _content;
		}

		public void OnDisplay(IPage lastPage) {
			if (_world != null) _component.UpdateContent(_world);
			else if (_isLoading) _component.UpdateLoading();
			else _component.UpdateError("World not found or loading failed.");
		}
	}
}