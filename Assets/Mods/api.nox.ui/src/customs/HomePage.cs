using System.Linq;
using api.nox.ui.histories;
using api.nox.ui.widgets;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.ui.pages
{
    public class HomePage : CustomPageHelper
    {
        protected override string GetKey() => "home";

        public HomePage() : base()
        {
            UISystem.Instance.Widgets.OnWidgetAdded.AddListener(OnWidgetUpdate);
            UISystem.Instance.Widgets.OnWidgetRemoved.AddListener(OnWidgetUpdate);
            UISystem.Instance.Widgets.OnWidgetChanged.AddListener(OnWidgetUpdate);
        }

        public override void Dispose()
        {
            UISystem.Instance.Widgets.OnWidgetAdded.RemoveListener(OnWidgetUpdate);
            UISystem.Instance.Widgets.OnWidgetRemoved.RemoveListener(OnWidgetUpdate);
            UISystem.Instance.Widgets.OnWidgetChanged.RemoveListener(OnWidgetUpdate);
            base.Dispose();
        }

        private Page[] GetAllCurrentHomePages()
            => UIClient.Instance.Cache
                .Select(menu => menu.GetCurrentPage())
                .Where(current => current.Key == GetKey())
                .ToArray();


        private void OnWidgetUpdate(Widget widget)
        {
            foreach (var page in GetAllCurrentHomePages())
                UpdateWidgets(page);
        }

        protected override void OnGotoEvent(int menuId, object[] args)
        {
            var page = new Page { Key = GetKey(), Context = args, MenuId = menuId };
            page.GetContent = transform => OnGetContent(page, transform);
            page.OnDisplay = (_, _) => UpdateWidgets(page);
            UISystem.Instance.Pages.SetPage(menuId, page);
        }

        private GameObject OnGetContent(Page page, RectTransform transform)
        {
            var asset = UISystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/home/content.prefab");
            asset.SetActive(false);
            var content = Object.Instantiate(asset, transform);
            Logger.LogDebug($"HomePage.OnGetContent: {asset.name} {content.name}");
            content.name = $"{GetKey()}_{content.name}";
            return content;
        }

        private void UpdateWidgets(Page page)
        {
            var home = page.Content.GetComponent<HomeComportment>();
            if (!home) return;
            home.UpdateWidgets(page, UISystem.Instance.Widgets.Cache.OrderBy(w => w.Weight).ToArray());
        }
    }
}