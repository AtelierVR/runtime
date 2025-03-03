using api.nox.ui.menus;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.ui.pages
{
    public class HelloComportment : MonoBehaviour
    {
        public Button homeButton;
        public Button sessionButton;
        public Button worldButton;

        internal void Initiate(Page page)
        {
            homeButton.onClick.AddListener(() => OnClick(page, "home"));
            sessionButton.onClick.AddListener(() => OnClick(page, "sessions"));
            worldButton.onClick.AddListener(() => OnClick(page, "custom"));
        }

        private void OnClick(Page page, string key)
            => UISystem.Instance.Pages.Goto(page.MenuId, key);
    }
}