using System.IO;
using UnityEngine;

namespace Nox.CCK.Utils
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
        public static float DefaultGravity = -9.81f;
        public static float DefaultJumpForce = 2.65f;
        public static double DefaultWalkSpeed = 2.5f;
        public static double DefaultCrouchMultiplier = 0.5f;
        public static double DefaultSprintMultiplier = 2f;
        public static float DefaultFlySpeed = .1f;

        public static Platform CurrentPlatform
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer:
                        return Platform.Windows;
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.OSXPlayer:
                        return Platform.MacOS;
                    case RuntimePlatform.LinuxEditor:
                    case RuntimePlatform.LinuxPlayer:
                        return Platform.Linux;
                    case RuntimePlatform.Android:
                        return Platform.Android;
                    case RuntimePlatform.IPhonePlayer:
                        return Platform.IOS;
                    default:
                        return Platform.None;
                }
            }
        }
    }
}