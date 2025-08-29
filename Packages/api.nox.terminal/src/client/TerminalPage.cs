using System;
using System.Collections.Generic;
using Nox.Terminals;
using Nox.UI;
using UnityEngine;

namespace api.nox.terminal.client {
	public class TerminalPage : IPage, IContext {
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
		private GameObject        _content;
		private TerminalComponent _component;

		private readonly Dictionary<string, object> _environments = new();

		internal string   _draft = string.Empty;
		internal string[] _auto  = Array.Empty<string>();

		public object[] GetContext()
			=> _context;

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			(_content, _component) = TerminalComponent.Generate(this, parent);
			return _content;
		}

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(_mId);

		public int GetId()
			=> _component.GetInstanceID();

		public Dictionary<string, object> GetEnvironments()
			=> _environments;


		public T GetEnvironment<T>(string key, T defaultValue = default)
			=> _environments.TryGetValue(key.ToUpperInvariant(), out var value) && value is T t
				? t
				: defaultValue;

		public void SetEnvironment(string key, object value) {
			key = key.ToUpperInvariant();
			if (value == null) _environments.Remove(key);
			else _environments[key] = value;
		}

		public void Print(string message) {
			if (!_component) return;
			_component.output.text += message;
			_component.output.ForceMeshUpdate();
		}

		public void PrintLn(string message)
			=> Print(message + Environment.NewLine);

		public void Clear() {
			if (!_component) return;
			_component.output.text = string.Empty;
			_component.output.ForceMeshUpdate();
		}

		public string GetTitle()
			=> _component.label.arguments.Length > 0
				? _component.label.arguments[0]
				: string.Empty;

		public void SetTitle(string title)
			=> _component.label.UpdateText("terminal.page.title", new[] { title });

		public object GetResult()
			=> GetEnvironment<object>("result");

		public void SetResult(object result)
			=> SetEnvironment("result", result);

		public bool CanPrinting()
			=> GetEnvironment("print_executing", true);

		public void SetPrinting(bool printing)
			=> SetEnvironment("print_executing", printing);
	}
}