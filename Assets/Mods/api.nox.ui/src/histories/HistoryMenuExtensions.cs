using api.nox.ui.menus;
using api.nox.ui.pages;

namespace api.nox.ui.histories
{
    public static class HistoryMenuExtensions
    {
        public static Page GetCurrentPage(this Menu menu)
            => menu.History.GetCurrent();
        
        public static void GoBackPage(this Menu menu)
            => menu.History.GoBack(menu);
        
        public static void GoForwardPage(this Menu menu)
            => menu.History.GoForward(menu);
        
        public static void AddPage(this Menu menu, Page page)
            => menu.History.Add(menu, page);

        public static void RestorePage(this Menu menu)
            => menu.History.Restore(menu);

        public static void ClearPage(this Menu menu)
            => menu.History.Clear(menu);
    }
}