using api.nox.search;
using Nox.CCK.Language;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.search.client
{
    public class HandlerComponent : MonoBehaviour
    {
        public RawImage icon;
        public TextLanguage text;
        public Button button;

        public Handler Data;
        
        

        public void Initiate(Handler data)
        {
            if (Data != null) return;
            UpdateData(data);
        }

        private void OnDestroy()
        {
            Data = null;
        }

        private void Awake()
        {
            icon.gameObject.SetActive(false);
            UpdateData(Data);
        }

        internal void UpdateData(Handler data)
        {
            Data = data;
            if (Data == null)
            {
                Logger.LogError("HandlerComponent: Data is not initiated.");
                return;
            }

            icon.gameObject.SetActive(true);
            if (!data.Icon)
            {
                icon.texture = null;
                icon.gameObject.SetActive(false);
            }
            else
            {
                icon.texture = data.Icon;
                icon.gameObject.SetActive(true);
            }

            text.UpdateText(data.TitleKey);
        }
    }
}