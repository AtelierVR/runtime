using System;
using System.Reflection;

namespace Nox.ModLoader.Mods
{
    public class FolderMod : Mod
    {
        public override AppDomain  GetAppDomain() {
            throw new NotImplementedException();
        }
        public override Assembly[] GetAssemblies() {
            throw new NotImplementedException();
        }
    }
}