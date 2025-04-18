using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.search.client
{
    public class SearchComponent : MonoBehaviour
    {
        public TMPro.TMP_InputField queryInput;
        public TextLanguage queryPlaceholder;
        public Image searchIcon;

        public GameObject searchContainer;
        public Button searchButton;
        public GameObject refreshContainer;
        public Button refreshButton;
        public GameObject cancelContainer;
        public Button cancelButton;

        public Button menuButton;
        public TextLanguage resultText;
        public RectTransform workerContainer;

        private SearchPage _page;

        private void OnDestroy()
        {
            queryInput.onValueChanged.RemoveListener(OnQueryChanged);
            searchButton.onClick.RemoveListener(OnSubmit);
            queryInput.onSubmit.RemoveListener(OnSubmit);
        }

        internal void Initiate(SearchPage page)
        {
            if (_page != null) return;
            _page = page;
        }

        private void Awake()
        {
            if (_page == null)
            {
                Logger.LogError("SearchComponent: SearchPage is not initiated.");
                return;
            }

            queryInput.onValueChanged.AddListener(OnQueryChanged);
            searchButton.onClick.AddListener(OnSubmit);
            queryInput.onSubmit.AddListener(OnSubmit);
        }


        private void OnSubmit(string query)
        {
            OnQueryChanged(query);
            OnSubmit();
        }

        private void OnQueryChanged(string query)
        {
            _page.Query = query.Trim();
            UpdateSearchButtons();
        }

        private void UpdateSearchButtons()
        {
            if (_page.IsFetching)
            {
                cancelContainer.SetActive(true);
                searchContainer.SetActive(false);
                refreshContainer.SetActive(false);
            }
            else if (_page.IsNewQuery)
            {
                searchContainer.SetActive(true);
                cancelContainer.SetActive(false);
                refreshContainer.SetActive(false);
            }
            else
            {
                refreshContainer.SetActive(true);
                cancelContainer.SetActive(false);
                searchContainer.SetActive(false);
            }
        }

        private void OnSubmit() => _page?.Submit().Forget();


        public void UpdateData()
        {
            queryInput.text = _page.Query.Trim();
            UpdateSearchButtons();
            var handler = _page.Handler;
            queryPlaceholder.UpdateText(string.IsNullOrEmpty(handler.PlaceholderKey)
                ? "search.query.placeholder"
                : handler.PlaceholderKey);
        }
    }
}