using UnityEngine;
using UnityEngine.UI;

namespace Mods.api.nox.game.ui.theming
{
    [RequireComponent(typeof(RectTransform))]
    public class ThemeModifier : MonoBehaviour
    {
        public string key;
        public ILayoutElement LayoutElement;

        public ThemeReference GetReference()
            => GetComponentInParent<ThemeReference>();

        public bool TryGetValue(ThemeType type, out ThemeValue value)
            => GetReference().theme.TryGetValue(key, type, out value);

        public void Apply()
        {
            if (LayoutElement == null) return;
            
            switch (LayoutElement)
            {
                case Text text:
                    Apply(text);
                    break;
                case Image image:
                    Apply(image);
                    break;
            }
        }

        private void Apply(Text text)
        {
            if (TryGetValue(ThemeType.Font, out var font))
                text.font = font.font;
        }

        private void Apply(Image image)
        {
            if (TryGetValue(ThemeType.Sprite, out var texture))
                image.sprite = texture.sprite;
            else if (TryGetValue(ThemeType.Color, out var color))
                image.color = color.color;
            else if (TryGetValue(ThemeType.Material, out var material))
                image.material = material.material;
            else if (TryGetValue(ThemeType.Number, out var number))
                image.fillAmount = number.number;
        }
    }
}