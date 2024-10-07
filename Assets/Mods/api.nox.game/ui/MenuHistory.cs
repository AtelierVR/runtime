using System.Collections.Generic;

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
            if (current < history.Count - 1)
                RemoveRange(current + 1, history.Count - current - 1);
            history.Add(tile);
            current = history.Count - 1;
            _menu.SetTile(tile, ot);
        }

        public void GoBack()
        {
            var ot = GetCurrent();
            if (current > 0)
                current--;
            _menu.SetTile(history[current], ot);
        }

        public void GoForward()
        {
            var ot = GetCurrent();
            if (current < history.Count - 1)
                current++;
            _menu.SetTile(history[current], ot);
        }

        public HistoryTile GetCurrent()
        {
            if (current >= 0 && current < history.Count)
                return history[current];
            return null;
        }

        private void RemoveRange(int v1, int v2)
        {
            while (v2-- > 0)
            {
                history[v1].Dispose();
                history.RemoveAt(v1);
                if (current > v1)
                    current--;
            }
        }

        public void Clear()
        {
            var ot = GetCurrent();
            RemoveRange(0, history.Count);
            current = -1;
            _menu.SetTile(null, ot);
        }
    }
}