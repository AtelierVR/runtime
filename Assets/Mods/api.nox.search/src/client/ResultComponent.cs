using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.search.client
{
    public class ResultComponent : MonoBehaviour
    {
        public RawImage icon;
        public TextLanguage text;
        public Button button;
        internal ResultData Data;
        public WorkerComponent workerComponent;

        public void Initiate(WorkerComponent wc, ResultData data)
        {
            workerComponent = wc;
            if (Data != null) return;
            UpdateData(data);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnClick);
            Data = null;
        }

        private void Awake()
        {
            icon.gameObject.SetActive(false);
            UpdateData(Data);
            button.onClick.AddListener(OnClick);
        }

        private INoxObject PageManager
            => SearchSystem.CoreAPI.ModAPI
                .GetMod("ui")
                .GetMains()
                .FirstOrDefault()
                ?.GetField("Pages");

        private INoxObject NetworkAPI
            => SearchSystem.CoreAPI.ModAPI
                .GetMod("network")
                .GetMains()
                .FirstOrDefault();

        private void OnClick()
            => PageManager.CallMethod("Goto", workerComponent.search.Page.MenuId, Data.GotoId, Data.GotoData);

        public void UpdateData(ResultData data)
        {
            Data = data;
            if (Data == null)
            {
                Logger.LogError("ResultComponent: Data is not initiated.");
                return;
            }

            text.UpdateText("search.result.title", new[] { Data.Title });
            UpdateImage(Data.ImageUrl).Forget();
        }

        private string _imageUrl;
        private bool _imageLoading;

        private async UniTask UpdateImage(string url)
        {
            if (_imageLoading) return;

            if (string.IsNullOrEmpty(url))
            {
                icon.texture = null;
                _imageUrl = null;
                icon.gameObject.SetActive(false);
                return;
            }

            _imageLoading = true;

            if (_imageUrl == url && icon.texture)
            {
                icon.gameObject.SetActive(true);
                return;
            }

            try
            {
                var texture = await NetworkAPI.CallAsyncMethod<Texture2D>("FetchTexture", url, null, null, null);
                if (texture)
                {
                    icon.texture = texture;
                    _imageUrl = url;
                    icon.gameObject.SetActive(true);
                }
                else
                {
                    Logger.LogError("ResultComponent: Failed to fetch texture.");
                    icon.gameObject.SetActive(false);
                    icon.texture = null;
                    _imageUrl = null;
                }
            }
            catch
            {
                Logger.LogError("ResultComponent: Exception while fetching texture.");
                icon.gameObject.SetActive(false);
                icon.texture = null;
                _imageUrl = null;
            }

            _imageLoading = false;
        }
    }
}