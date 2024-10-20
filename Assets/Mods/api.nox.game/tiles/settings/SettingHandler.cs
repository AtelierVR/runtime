using System;
using UnityEngine;

namespace api.nox.game.Settings
{
    
    public class SettingHandler 
    {
        public string id;
        public string text_key;
        public string title_key;
        public Func<SettingPage[]> GetPages;
    }

    public class SettingPage
    {
        public string title_key;
        public string description_key;
        public Texture2D icon;

        public SettingGroup[] groups;
    }
    

    public class SettingGroup
    {
        public string title_key;
        public string description_key;
        public SettingEntry[] entries;
    }

    public class SettingEntry
    {
        public string title_key;
        public string description_key;
        public Texture2D icon;
    }

    public class ToggleSettingEntry : SettingEntry
    {
        public bool value;
    }


}