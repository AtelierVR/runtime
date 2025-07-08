using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.ui.colors
{
    public class ButtonThemeSetter : ThemeSetter
    {
        public Button button;

        public string normalColor = "";
        public string highlightedColor = "";
        public string pressedColor = "";
        public string selectedColor = "";
        public string disabledColor = "";

        public override void UpdateColors()
        {
            base.UpdateColors();

            if (!button) return;

            var block = button.colors;
            var normalPath = string.IsNullOrEmpty(normalColor)
                ? "primary-200-0"
                : normalColor;
            var highlightedPath = string.IsNullOrEmpty(highlightedColor)
                ? "primary-300"
                : highlightedColor;
            var pressedPath = string.IsNullOrEmpty(pressedColor)
                ? "primary-400"
                : pressedColor;
            var selectedPath = string.IsNullOrEmpty(selectedColor)
                ? "primary-400"
                : selectedColor;
            var disabledPath = string.IsNullOrEmpty(disabledColor)
                ? "primary-950"
                : disabledColor;
            
            if (TryColor(normalPath, out var c))
                block.normalColor = c;
            if (TryColor(highlightedPath, out c))
                block.highlightedColor = c;
            if (TryColor(pressedPath, out c))
                block.pressedColor = c;
            if (TryColor(selectedPath, out c))
                block.selectedColor = c;
            if (TryColor(disabledPath, out c))
                block.disabledColor = c;
            
            button.colors = block;
        }
    }
}