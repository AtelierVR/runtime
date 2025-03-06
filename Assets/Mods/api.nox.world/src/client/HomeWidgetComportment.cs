using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.world
{
    public class HomeWidgetComportment : MonoBehaviour
    {
        public GameObject thumbnailContainer;
        public RawImage thumbnail;
        public Button button;

        private void Start()
        {
            WorldClient.Instance.OnHomeUpdated.AddListener(UpdateContent);
        }

        private void OnDestroy()
        {
            WorldClient.Instance.OnHomeUpdated.RemoveListener(UpdateContent);
        }

        internal void UpdateContent(INoxObject home)
        {
        }
    }
}