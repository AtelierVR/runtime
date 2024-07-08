using System.IO;
using UnityEngine;

namespace Nox.CCK
{
    public class Constants
    {
        public static ushort ProtocolVersion => 1;
        public static string GameIdentifier => "AVR";
        public static string GameAppDataPath
        {
            get
            {
                var dir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + "/.avr/";
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }
        
        public static Engine CurrentEngine => Engine.Unity;

        public static float IntervalLantencyRequest = 2.5f;

        public static Platfrom CurrentPlatform
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer:
                        return Platfrom.Windows;
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.OSXPlayer:
                        return Platfrom.MacOS;
                    case RuntimePlatform.LinuxEditor:
                    case RuntimePlatform.LinuxPlayer:
                        return Platfrom.Linux;
                    case RuntimePlatform.Android:
                        return Platfrom.Android;
                    case RuntimePlatform.IPhonePlayer:
                        return Platfrom.IOS;
                    default:
                        return Platfrom.None;
                }
            }
        }
    }
}