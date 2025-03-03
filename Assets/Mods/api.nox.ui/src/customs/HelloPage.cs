using UnityEngine;

namespace api.nox.ui.pages
{
    public class HelloPage : CustomPageHelper
    {
        protected override string GetKey() => "hello";

        protected override void OnGotoEvent(int menuId, object[] args)
        {
            var page = new Page { Key = GetKey(), Context = args, MenuId = menuId };
            page.GetContent = transform => OnGetContent(page, transform);

            UISystem.Instance.Pages.SetPage(menuId, page);
        }

        private GameObject OnGetContent(Page page, RectTransform transform)
        {
            var asset = UISystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/hello/content.prefab");
            asset.SetActive(false);
            var content = Object.Instantiate(asset, transform);
            content.name = $"{GetKey()}_{content.name}";
            var comportment = content.GetComponent<HelloComportment>();
            comportment.Initiate(page);
            return content;
        }
    }
}