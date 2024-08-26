using System;
using Nox.CCK.Mods;

namespace api.nox.network
{
    public class AuthToken : ShareObject
    {
        [ShareObjectExport] public string token;
        [ShareObjectExport] public bool isIntegrity;

        public string ToHeader() => isIntegrity ? $"Integrity {token}" : $"Bearer {token}";

        [ShareObjectExport] public Func<string> SharedHeader;

        public void BeforeExport()
        {
            SharedHeader = () => ToHeader();
        }

        public void AfterExport()
        {
            SharedHeader = null;
        }
    }
}