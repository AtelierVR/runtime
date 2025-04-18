using System;
using System.Collections.Generic;
using api.nox.ui.histories;
using api.nox.ui.menus;
using api.nox.ui.pages;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;

namespace api.nox.ui.Mods.api.nox.ui.src.pages
{
    public class PageManager : INoxObject, IDisposable
    {
        public PageManager()
            => _events = new[]
            {
                UISystem.CoreAPI.EventAPI.Subscribe("display_page", OnEventDisplay),
                UISystem.CoreAPI.EventAPI.Subscribe("goto_action", OnEventGoto),
            };

        private void OnEventGoto(EventData context)
        {
            if (!context.TryGet(0, out int menuId) || !context.TryGet(1, out string action))
                return;
            var menu = UIClient.Instance.Get<Menu>(menuId);
            if (!menu) return;
            switch (action)
            {
                case "move":
                    if (!context.TryGet(0, out int move)) return;
                    menu.MovePage(move);
                    break;
                case "back":
                    menu.GoBackPage();
                    break;
                case "forward":
                    menu.GoForwardPage();
                    break;
            }
        }

        private void OnEventDisplay(EventData context)
        {
            if (!context.TryGet(0, out int menuId) || !context.TryGet(1, out Dictionary<string, object> data))
                return;
            var page = Page.From(data);
            if (page == null) return;
            SetPage(menuId, page);
        }

        [NoxPublic(NoxAccess.Method)]
        public void Goto(int menuId, string pageKey, object[] args)
        {
            var list = new List<object> { menuId, pageKey };
            list.AddRange(args);
            UISystem.CoreAPI.EventAPI.Emit("goto_page", list.ToArray());
        }

        internal void SetPage(int menuId, Page page)
        {
            var menu = UIClient.Instance.Get<Menu>(menuId);
            if (!menu) return;

            menu.History.Add(menu, page);
        }

        private readonly EventSubscription[] _events;

        public void Dispose()
        {
            foreach (var ev in _events)
                UISystem.CoreAPI.EventAPI.Unsubscribe(ev);
        }
    }
}