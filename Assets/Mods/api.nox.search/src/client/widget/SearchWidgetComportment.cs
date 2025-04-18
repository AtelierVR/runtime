using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.search.widget
{
    public class SearchWidgetComportment : MonoBehaviour
    {
        public Button button;
        public int menuId;

        private void Start()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            SearchClient.UISystem
                .GetField("Pages")
                .InvokeMethod("Goto", menuId, "search", new object[] { });
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnClick);
        }

        internal void UpdateContent()
        {
        }
    }
}