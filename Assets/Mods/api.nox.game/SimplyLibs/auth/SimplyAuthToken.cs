using System;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyAuthToken : ShareObject
    {
        [ShareObjectImport] public string token;
        [ShareObjectImport] public bool isIntegrity;

        [ShareObjectImport] public Func<string> SharedHeader;
        public string ToHeader() => isIntegrity ? $"Integrity {token}" : $"Bearer {token}";
    }
}