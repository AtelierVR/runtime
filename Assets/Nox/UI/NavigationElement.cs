using SpaceBear.VRUI;
using UnityEngine;
using UnityEngine.UI;

namespace Nox.UI
{
    public class NavigationElement : MonoBehaviour
    {
        public string Text
        {
            get => transform.Find("Text").GetComponent<Text>().text;
            set => transform.Find("Text").GetComponent<Text>().text = value;
        }

        public Texture2D Icon
        {
            get => transform.Find("Icon").GetComponent<RawImage>().texture as Texture2D;
            set => transform.Find("Icon").GetComponent<RawImage>().texture = value;
        }

        public static NavigationElement Create(string text, Texture2D icon)
        {
            var prefab = Resources.Load<GameObject>("nox.ui.navigation.element");
            var element = Instantiate(prefab, Menu.Navigation).GetComponent<NavigationElement>();
            var component = element.GetComponent<VRUIControlBarButton>();
            component.onClick.AddListener(element.OnClick);
            element.Text = text;
            element.Icon = icon;
            return element;
        }

        public void Destroy() => Destroy(gameObject);

        public void OnClick()
        {
            Debug.Log($"Clicked on {Text}");
        }
    }
}