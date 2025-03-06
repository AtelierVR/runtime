using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.world
{
    public class HomeWidgetComportment : MonoBehaviour
    {
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
            Logger.Log($"HomeWidgetComportment.UpdateContent({home})");
        }
    }
}