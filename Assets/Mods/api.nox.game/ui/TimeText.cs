using Nox.CCK;
using UnityEngine;

namespace api.nox.game
{
    public class TimeText : MonoBehaviour
    {
        private TextLanguage text => GetComponent<TextLanguage>();
        void Update()
        {
            text.arguments = new string[] { 
                // hours 12
                System.DateTime.Now.ToString("hh"),
                // hours 24
                System.DateTime.Now.ToString("HH"),
                // minutes
                System.DateTime.Now.ToString("mm"),
                // seconds
                System.DateTime.Now.ToString("ss"),
                // ms
                System.DateTime.Now.ToString("fff"),
                // am-pm
                System.DateTime.Now.ToString("tt")
            };
            text.UpdateText();
        }
    }
}