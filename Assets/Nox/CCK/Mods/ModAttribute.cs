using System;
using UnityEngine;

namespace Nox.CCK.Mods
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModAttribute : Attribute
    {
        public string Name;
        public string Version;
        public string Author;

        public ModAttribute(string name, string version, string author = null)
        {
            Name = name;
            Version = version;
            Author = author;
        }
    }
}