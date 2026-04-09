using System;
using api.nox.server.network;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Events;
using Nox.Servers;
using Nox.UI;
using Nox.Users;
using UnityEngine;

namespace api.nox.server.client {
	public class ServerPage : IPage {
		static internal string GetStaticKey()
			=> "server";

		public string GetKey()
			=> GetStaticKey();

		internal int             MId;
		private  object[]        _context;
		private  GameObject      _content;
		private  ServerComponent _component;
		private  string          _address;
		public   IServer         Server;
		private  bool            _isLoading;

		private EventSubscription[] _events = Array.Empty<EventSubscription>();

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

		internal bool IsHost(ICurrentUser current = null)
			=> (current ?? Client.UserAPI.Current)?.Server == _address;

		static internal IPage OnGotoAction(IMenu menu, object[] context) {
			if (!T(context, 0, out string type)) return null;
			switch (type) {
				case "address" when T(context, 1, out string a0):
					return OnPageByAddress(menu, context, a0);
				case "server" when T(context, 1, out IServer w0):
					return OnPageByServer(menu, context, w0);
			}

			return null;
		}

		private static ServerPage OnPageByAddress(IMenu menu, object[] context, string address) {
			var page = new ServerPage {
				MId      = menu.Id,
				_context = context,
				_address = address,
				Server   = null,
			};
			page.Refresh(true).Forget();
			return page;
		}

		private static ServerPage OnPageByServer(IMenu menu, object[] context, IServer server) {
			var page = new ServerPage {
				MId      = menu.Id,
				_context = context,
				_address = server.Address,
				Server   = server,
			};
			return page;
		}

		private async UniTask Refresh(bool load) {
			if (_isLoading) return;
			_isLoading = true;
			Server     = await Network.Fetch(_address);
			_isLoading = false;
			if (load) _component.UpdateContent(Server);
		}

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(MId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			(_content, _component) = ServerComponent.Generate(this, parent);
			_component.UpdateLoading();
			return _content;
		}

		public void OnOpen(IPage lastPage) {
			_events = new[] {
				Main.Instance.CoreAPI.EventAPI.Subscribe("server_fetch", OnServerUpdate),
			};
		}

		private void OnServerUpdate(EventData context) {
			Server = context.TryGet(0, out IServer srv) ? srv : Server;
			if (Server != null) _component.UpdateContent(Server);
		}

		public void OnDisplay(IPage lastPage) {
			if (Server != null) _component.UpdateContent(Server);
			else if (_isLoading) _component.UpdateLoading();
			else _component.UpdateError("World not found or loading failed.");
		}

		public void OnRemove() {
			foreach (var ev in _events)
				Main.Instance.CoreAPI.EventAPI.Unsubscribe(ev);
		}
	}
}