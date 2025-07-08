using Nox.CCK.Utils;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace api.nox.ui.colors
{
    public class ImageThemeSetter : ThemeSetter
    {
        public string path = "";

        public Image image;

        public override void UpdateColors()
        {
            base.UpdateColors();
            
            var p = string.IsNullOrEmpty(path)
                ? "secondary-800"
                : path;
            
            if (image && TryColor(p, out var c))
                image.color = c;
            else Logger.LogDebug($"No color found for {p}", this);
        }
    }
}