using System;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyRelayResponseEnter : ShareObject
    {

        public SimplyRelayEnterResult Result;
        [ShareObjectImport] public string Reason;
        [ShareObjectImport] public DateTime Expiration;
        [ShareObjectImport] public byte MaxTps;
        [ShareObjectImport] public byte SharedResult;
        [ShareObjectImport] public Func<bool> SharedIsSuccess;
        public bool IsSuccess => SharedIsSuccess();

        public void AfterImport()
        {
            Result = (SimplyRelayEnterResult)SharedResult;
            SharedResult = 0;
        }
    }
}