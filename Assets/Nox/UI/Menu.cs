using Nox.CCK;
using UnityEngine;

namespace Nox.UI
{
    [RequireComponent(typeof(Canvas))]
    public class Menu : MonoBehaviour
    {
        public static Menu Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public static RectTransform Navigation => Finder.FindComponent<RectTransform>("navigation", Instance.gameObject);
        public static RectTransform Content => Finder.FindComponent<RectTransform>("body", Instance.gameObject);
        public static RectTransform TopBar => Finder.FindComponent<RectTransform>("topbar", Instance.gameObject);
    }
}