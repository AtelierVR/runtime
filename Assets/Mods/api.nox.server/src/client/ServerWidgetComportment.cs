using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.server
{
    public class ServerWidgetComportment : MonoBehaviour
    {
        public GameObject iconContainer;
        public RawImage icon;
        public Button button;

        private void Start()
            => ServerSystem.Instance.OnServerUpdated.AddListener(UpdateContent);

        private void OnDestroy()
            => ServerSystem.Instance.OnServerUpdated.RemoveListener(UpdateContent);

        internal void UpdateContent(INoxObject user)
        {
            button.interactable = user != null;

            if (user == null)
            {
                icon.texture = null;
                iconContainer.SetActive(false);
                return;
            }
            
            var strIcon = user.GetField<string>("icon");

            if (!string.IsNullOrEmpty(strIcon))
                FetchTexture(icon, iconContainer, strIcon).Forget();
        }

        private static async UniTask FetchTexture(RawImage image, GameObject container, string url)
        {
            try
            {
                var texture = await ServerSystem.NetworkAPI
                    .CallAsyncMethod<Texture2D>("FetchTexture", url, null, null, null);
                if (!texture)
                {
                    image.texture = null;
                    container.SetActive(false);
                    return;
                }

                image.texture = texture;
                container.SetActive(true);
            }
            catch
            {
                // ignored
            }
        }
    }
}