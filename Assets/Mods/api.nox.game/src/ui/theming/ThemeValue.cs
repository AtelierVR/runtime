using UnityEngine;

namespace Mods.api.nox.game.ui.theming
{
    [System.Serializable]
    public class ThemeValue
    {
        [SerializeField] public string key;
        [SerializeField] public ThemeType type;
        [SerializeField] public Color color;
        [SerializeField] public Sprite sprite;
        [SerializeField] public Font font;
        [SerializeField] public float number;
        [SerializeField] public Material material;
    }
}