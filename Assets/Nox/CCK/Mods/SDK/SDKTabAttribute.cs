using System;

namespace Nox.CCK.Mods
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CCKTabAttribute : MethodAttribute
    {
        public string Name;
        public string Id;
        public CCKTabFlags Flags;

        public CCKTabAttribute(string id, string name = null, CCKTabFlags flags = CCKTabFlags.None) : base()
        {
            Id = id;
            Name = name;
            Flags = flags;
        }
    }

    [Flags]
    public enum CCKTabFlags
    {
        None = 0,
        Hidden = 1 << 0,
    }
}