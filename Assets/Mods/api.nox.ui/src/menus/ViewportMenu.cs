using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.ui.menus
{
    [RequireComponent(typeof(RectTransform))]
    public class ViewportMenu : Menu
    {
        public static ViewportMenu Create(RectTransform parent)
        {
            var asset = UISystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/viewport_menu.prefab");
            Logger.LogDebug($"Creating ViewportMenu from {asset} in {parent}");
            var instance = Instantiate(asset, parent.transform);
            var menu = instance.GetComponent<ViewportMenu>();
            instance.name = $"[{menu.GetType()}_{menu.GetInstanceID()}]";
            return menu;
        }
    }
}