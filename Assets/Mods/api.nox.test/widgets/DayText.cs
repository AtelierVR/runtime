using Nox.CCK;
using UnityEngine;

namespace api.nox.test
{
    public class DayText : MonoBehaviour
    {
        private TextLanguage text => GetComponent<TextLanguage>();
        public string keyformat = "day.{0}";
        void Update()
        {
            text.key = string.Format(keyformat, System.DateTime.Now.DayOfWeek.ToString().ToLower());
            text.UpdateText();
        }
    }
}