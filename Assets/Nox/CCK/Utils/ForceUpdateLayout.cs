using UnityEngine;
using UnityEngine.UI;

namespace Nox.CCK.Utils
{
    public interface IUpdateLayout
    {
        void UpdateLayout()
        {
        }
    }

    public class ForceUpdateLayout
    {
        public static void UpdateManually(GameObject go) 
            => UpdateManually(go.GetComponent<RectTransform>());

        public static void UpdateManually(RectTransform rect)
        {
            if (!rect || !rect.gameObject.activeInHierarchy) return;

            foreach (UnityEngine.Transform child in rect)
                if (child.TryGetComponent<RectTransform>(out var rec))
                    UpdateManually(rec);

            var rectTransform = rect.GetComponent<RectTransform>();
            var contentSizeFitter = rect.GetComponent<ContentSizeFitter>();
            var layoutGroup = rect.GetComponent<LayoutGroup>();

            if (contentSizeFitter)
            {
                contentSizeFitter.SetLayoutHorizontal();
                contentSizeFitter.SetLayoutVertical();
            }

            if (layoutGroup)
            {
                layoutGroup.CalculateLayoutInputHorizontal();
                layoutGroup.CalculateLayoutInputVertical();
                layoutGroup.SetLayoutHorizontal();
                layoutGroup.SetLayoutVertical();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

            foreach (var child in rect.GetComponents<IUpdateLayout>())
                child.UpdateLayout();
        }
    }
}