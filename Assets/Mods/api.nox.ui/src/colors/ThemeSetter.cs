using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace api.nox.ui
{
    public class ThemeSetter : MonoBehaviour, IUpdateLayout
    {
        public static Theme Main;
        public static UnityEvent<Theme> OnMainChanged = new();
        public Theme theme;
    }
}