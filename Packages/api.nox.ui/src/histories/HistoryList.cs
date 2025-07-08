using System.Collections.Generic;
using api.nox.ui.menus;
using Cysharp.Threading.Tasks;
using Nox.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.ui.histories {
	public class HistoryList {
		private readonly Menu        _menu;
		private readonly List<IPage> _cache   = new();
		private          int         _current = -1;

		public HistoryList(Menu menu)
			=> _menu = menu;

		public void Add(IPage page) {
			var crt = GetCurrent();
			Logger.Log($"Add {page.GetKey()} to history {_current} {_cache.Count}");
			if (_current < _cache.Count - 1)
				RemoveRange(_current + 1, _cache.Count - _current - 1);
			_cache.Add(page);
			_current = _cache.Count - 1;
			_menu.SetPage(page, crt, PageFlags.IsNew | PageFlags.IsForward).Forget();
		}

		public void Move(int move) {
			if (move == 0) return;
			if (move < 0) GoBack(-move);
			else GoForward(move);
		}

		public void GoBack(int count = 1) {
			var crt = GetCurrent();
			while (count-- > 0 && _current > 0)
				_current--;
			_menu.SetPage(_cache[_current], crt, PageFlags.IsRestore | PageFlags.IsBack).Forget();;
		}

		public void GoForward(int count = 1) {
			var crt = GetCurrent();
			while (count-- > 0 && _current < _cache.Count - 1)
				_current++;
			_menu.SetPage(_cache[_current], crt, PageFlags.IsRestore | PageFlags.IsForward).Forget();;
		}

		public IPage GetCurrent() {
			if (_current >= 0 && _current < _cache.Count)
				return _cache[_current];
			return null;
		}

		private void RemoveRange(int v1, int v2) {
			var old = GetCurrent();
			while (v2-- > 0) {
				Logger.Log($"Remove {v1} from history {_current} {_cache.Count}");
				_cache[v1].OnRemove();
				_cache.RemoveAt(v1);
				if (_current > v1)
					_current--;
			}

			var cur = GetCurrent();
			if (old != cur)
				_menu.SetPage(cur, old, PageFlags.IsRestore | PageFlags.IsBack).Forget();;
		}

		public void Clear() {
			var crt = GetCurrent();
			RemoveRange(0, _cache.Count);
			_current = -1;
			_menu.SetPage(null, crt, PageFlags.IsBack).Forget();;
		}

		internal void Restore() {
			var crt = GetCurrent();
			_menu.SetPage(crt, crt, PageFlags.IsRestore).Forget();;
		}
	}
}