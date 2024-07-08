// make atribute for CallMethod and CallMethodWithReturn

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nox.CCK.Mods
{
    public class Attribute : System.Attribute
    {
        public AttributeLevel Level = AttributeLevel.Private;
    }

    public enum AttributeLevel
    {
        Private,
        Public,
        Restricted
    }
}