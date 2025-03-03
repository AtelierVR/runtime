using UnityEngine;

namespace Mods.api.nox.ui.menus
{
    [RequireComponent(typeof(RectTransform))]
    public class ViewportMenu : Menu
    {
        public static ViewportMenu Create(Transform parent)
        {
            var asset = UISystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/viewport_menu");
            var instance = Instantiate(asset, parent);
            var menu = instance.GetComponent<ViewportMenu>();
            instance.name = $"[{menu.GetType()}_{menu.GetInstanceID()}]";
            return menu;
        }
    }
}