using System;
using UnityEngine;

namespace api.nox.game.UI
{
    [Serializable]
    public class NavigationMenuItem
    {
        public string key;
        public Texture2D icon;
        public string text;
        public string[] text_arguments;
        public string tooltip;
        public string[] tooltip_arguments;
        public NavigationMenuItemExecutionEnum execution_type;
        public string execution;
        public object[] execution_arguments;
        public NavigationMenuItemActiveFlags flags;
    }

    [Flags]
    public enum NavigationMenuItemActiveFlags
    {
        None = 0,
        Interactable = 1,
        Enabled = 2
    }

    public enum NavigationMenuItemExecutionEnum
    {
        None,
        GotoTile,
        SendEvent,
        MenuAction
    }
}