using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
namespace api.nox.game
{
    public class ForceUpdateLayout
    {
        public static void UpdateManually(GameObject go) => UpdateManually(go.GetComponent<RectTransform>());

        public static void UpdateManually(RectTransform rect)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy) return;

            foreach (Transform child in rect)
                if (child.TryGetComponent<RectTransform>(out var rec)) UpdateManually(rec);
            var rectTransform = rect.GetComponent<RectTransform>();
            var contentSizeFitter = rect.GetComponent<ContentSizeFitter>();
            var layoutGroup = rect.GetComponent<LayoutGroup>();

            if (contentSizeFitter != null)
            {
                contentSizeFitter.SetLayoutHorizontal();
                contentSizeFitter.SetLayoutVertical();
            }

            if (layoutGroup != null)
            {
                layoutGroup.CalculateLayoutInputHorizontal();
                layoutGroup.CalculateLayoutInputVertical();
                layoutGroup.SetLayoutHorizontal();
                layoutGroup.SetLayoutVertical();
            }

            foreach (Transform child in rect)
                if (child.TryGetComponent<AutoFitter>(out var rec)) rec.Fit();

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

            if (rect.TryGetComponent<MenuGridder>(out var menugridder))
            {
                rect.gameObject.SetActive(false);
                UniTask.DelayFrame(1).ContinueWith(() =>
                {
                    rect.gameObject.SetActive(true);
                    menugridder.UpdateContent();
                }).Forget();
            }
        }
    }
}
