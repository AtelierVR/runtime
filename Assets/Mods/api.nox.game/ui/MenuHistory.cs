using System.Collections.Generic;
using UnityEngine;

namespace api.nox.game.UI
{
    public class MenuHistory
    {
        private Menu _menu;
        internal MenuHistory(Menu menu) => _menu = menu;

        public List<HistoryTile> history = new();
        public int current = -1;

        public void Add(HistoryTile tile)
        {
            var ot = GetCurrent();
            Debug.Log($"Add {tile.id} to history {current} {history.Count}");
            if (current < history.Count - 1)
                RemoveRange(current + 1, history.Count - current - 1);
            history.Add(tile);
            current = history.Count - 1;
            _menu.SetTile(tile, ot, SetTileFlags.IsNew | SetTileFlags.IsForward);
        }

        public void GoBack()
        {
            var ot = GetCurrent();
            if (current > 0)
                current--;
            else return;
            _menu.SetTile(history[current], ot, SetTileFlags.IsRestore | SetTileFlags.IsBack);
        }

        public void GoForward()
        {
            var ot = GetCurrent();
            if (current < history.Count - 1)
                current++;
            else return;
            _menu.SetTile(history[current], ot, SetTileFlags.IsRestore | SetTileFlags.IsForward);
        }

        public HistoryTile GetCurrent()
        {
            if (current >= 0 && current < history.Count)
                return history[current];
            return null;
        }

        private void RemoveRange(int v1, int v2)
        {
            var old = GetCurrent();
            while (v2-- > 0)
            {
                Debug.Log($"Remove {v1} from history {current} {history.Count}");
                history[v1].Dispose();
                history.RemoveAt(v1);
                if (current > v1)
                    current--;
            }

            var cur = GetCurrent();
            if (old != cur)
                _menu.SetTile(cur, old, SetTileFlags.IsRestore | SetTileFlags.IsBack);
        }

        public void Clear()
        {
            var ot = GetCurrent();
            RemoveRange(0, history.Count);
            current = -1;
            _menu.SetTile(null, ot, SetTileFlags.None | SetTileFlags.IsBack);
        }

        internal void Restore()
        {
            var ot = GetCurrent();
            _menu.SetTile(ot, ot, SetTileFlags.IsRestore);
        }
    }
}