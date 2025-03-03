using UnityEngine;

namespace Mods.api.nox.ui.menus
{
    public abstract class Menu : MonoBehaviour
    {
        public virtual void Show() => gameObject.SetActive(true);
        public virtual void Hide() => gameObject.SetActive(false);
        public virtual bool IsVisible() => gameObject.activeSelf;
    }
}