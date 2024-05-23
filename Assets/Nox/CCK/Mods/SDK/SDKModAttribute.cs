using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Nox.CCK.Mods
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CCKModAttribute : ModAttribute
    {
        public CCKModAttribute(string name, string version) : base(name, version) { }
    }
}