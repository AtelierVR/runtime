using System.Collections.Generic;
using api.nox.ui.menus;
using api.nox.ui.pages;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.ui.histories
{
    public class HistoryList
    {
        private readonly List<Page> _cache = new();
        private int _current = -1;

        public void Add(Menu menu, Page page)
        {
            var crt = GetCurrent();
            Logger.Log($"Add {page.Key} to history {_current} {_cache.Count}");
            if (_current < _cache.Count - 1)
                RemoveRange(menu, _current + 1, _cache.Count - _current - 1);
            _cache.Add(page);
            _current = _cache.Count - 1;
            menu.SetPage(page, crt, PageFlags.IsNew | PageFlags.IsForward);
        }

        public void Move(Menu menu, int move)
        {
            if (move == 0) return;
            if (move < 0) GoBack(menu, -move);
            else GoForward(menu, move);
        }

        public void GoBack(Menu menu, int count = 1)
        {
            var crt = GetCurrent();
            while (count-- > 0 && _current > 0)
                _current--;
            menu.SetPage(_cache[_current], crt, PageFlags.IsRestore | PageFlags.IsBack);
        }

        public void GoForward(Menu menu, int count = 1)
        {
            var crt = GetCurrent();
            while (count-- > 0 && _current < _cache.Count - 1)
                _current++;
            menu.SetPage(_cache[_current], crt, PageFlags.IsRestore | PageFlags.IsForward);
        }

        public Page GetCurrent()
        {
            if (_current >= 0 && _current < _cache.Count)
                return _cache[_current];
            return null;
        }

        private void RemoveRange(Menu menu, int v1, int v2)
        {
            var old = GetCurrent();
            while (v2-- > 0)
            {
                Logger.Log($"Remove {v1} from history {_current} {_cache.Count}");
                _cache[v1].Dispose();
                _cache.RemoveAt(v1);
                if (_current > v1)
                    _current--;
            }

            var cur = GetCurrent();
            if (old != cur)
                menu.SetPage(cur, old, PageFlags.IsRestore | PageFlags.IsBack);
        }

        public void Clear(Menu menu)
        {
            var crt = GetCurrent();
            RemoveRange(menu, 0, _cache.Count);
            _current = -1;
            menu.SetPage(null, crt, PageFlags.IsBack);
        }

        internal void Restore(Menu menu)
        {
            var crt = GetCurrent();
            menu.SetPage(crt, crt, PageFlags.IsRestore);
        }
    }
}