using System;
using Nox.Instances;
using Nox.UI;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.instance.client {
	public class InstancePage : IPage {
		internal static string GetStaticKey()
			=> "instance";

		public string GetKey()
			=> GetStaticKey();

		private int               _mId;
		private object[]          _context;
		private GameObject        _content;
		private InstanceComponent _component;
		public  IInstance         Instance;

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
				case "instance" when T(context, 1, out IInstance instance):
					return OnPageByInstance(menu, context, instance);
			}

			return null;
		}

		private static InstancePage OnPageByInstance(IMenu menu, object[] context, IInstance instance) {
			var page = new InstancePage {
				_mId     = menu.GetId(),
				_context = context,
				Instance = instance
			};
			return page;
		}

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(_mId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			Logger.LogDebug($"Creating content for instance page", parent);
			(_content, _component) = InstanceComponent.Generate(this, parent);
			Logger.LogDebug($"Created content for instance page", parent);
			return _content;
		}

		public void OnOpen(IPage lastPage) {
			// Handle page opening logic if needed
		}

		public void OnDisplay(IPage lastPage) {
			// Handle page didplay logic if needed
		}

		public void OnRemove() {
			// Handle cleanup if needed
		}

		public void OnRefresh() {
			// Handle refresh logic if needed
		}
	}
}