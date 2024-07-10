using Nox.CCK;
using UnityEngine;

namespace api.nox.test
{
    public class DateText : MonoBehaviour
    {
        private TextLanguage text => GetComponent<TextLanguage>();
        void Update()
        {
            text.arguments = new string[] { 
                // year
                System.DateTime.Now.ToString("yyyy"),
                // month
                System.DateTime.Now.ToString("MM"),
                // day
                System.DateTime.Now.ToString("dd")
            };
            text.UpdateText();
        }
    }
}