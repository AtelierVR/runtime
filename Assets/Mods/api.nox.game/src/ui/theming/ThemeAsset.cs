using System.Linq;
using UnityEngine;

namespace Mods.api.nox.game.ui.theming
{
    [CreateAssetMenu(fileName = "Theme", menuName = "Nox/Theme")]
    public class ThemeAsset : ScriptableObject
    {
        [SerializeField] public ThemeValue[] values;

        public ThemeValue GetValue(string key)
            => values.FirstOrDefault(value => value.key == key);

        public bool TryGetValue(string key, ThemeType type, out ThemeValue value)
        {
            value = GetValue(key);
            return value != null && value.type == type;
        }
    }
}