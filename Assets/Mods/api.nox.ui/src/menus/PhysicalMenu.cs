using UnityEngine;

namespace api.nox.ui.menus
{
    [RequireComponent(typeof(Transform))]
    public class PhysicalMenu : Menu
    {
        public static PhysicalMenu Create(Vector3 position, Quaternion rotation)
        {
            var asset = UISystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/physical_menu.prefab");
            var instance = Instantiate(asset, position, rotation);
            var menu = instance.GetComponent<PhysicalMenu>();
            instance.name = $"[{menu.GetType()}_{menu.GetInstanceID()}]";
            return menu;
        }
    }
}