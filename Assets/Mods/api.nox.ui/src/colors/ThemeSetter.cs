using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.ui.colors
{
    public class ThemeSetter : MonoBehaviour
    {
        public static Theme Main;
        public static UnityEvent<Theme> OnMainChanged = new();
        public Theme theme;

        public void UpdateLayout()
            => UpdateColors();

        public virtual void UpdateColors()
        {
            Logger.LogDebug($"UpdateColors {name}", this);
        }

        static string[] GetPath(string colorPath)
            => colorPath.Split("-");

        public bool TryColor(string colorPath, out Color color)
            => TryGetColor(colorPath, out color, theme);


        static bool TryGetColor(string colorPath, out Color color, params Theme[] themes)
        {
            var path = GetPath(colorPath);
            if (path.Length < 2)
            {
                Logger.LogError($"Invalid color path: {colorPath}");
                color = default;
                return false;
            }

            var (name, value) = (path[0], path[1]);
            var opacity = -1;
            var useOpacity = path.Length > 2 && int.TryParse(path[2], out opacity);
            
            foreach (var theme in themes)
                if (theme && theme.TryGetColor(name, value, out color))
                {
                    if (useOpacity) color.a = opacity;
                    return true;
                }
                else if (theme)
                    Logger.LogError($"Theme {theme.name} does not contain color {name}:{value}");

            if (Main && Main.TryGetColor(name, value, out color))
            {
                if (useOpacity) color.a = opacity;
                return true;
            }

            if (Main) Logger.LogError($"Main theme does not contain color {name}:{value}");

            if (Theme.TryGetColor(Theme.DefaultColors, name, value, out color))
            {
                if (useOpacity) color.a = opacity;
                return true;
            }

            Logger.LogError($"Default theme does not contain color {name}:{value}");

            color = default;
            return false;
        }
    }
}