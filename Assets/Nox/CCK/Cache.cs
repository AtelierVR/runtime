using System;
using System.IO;
using NUnit.Framework;

namespace Nox.CCK
{
    public class Cache
    {

        public static string GetPath() => Path.Combine(Constants.GameAppDataPath, "cache/");
        public static string GetPath(string hash) => Path.Combine(GetPath(), hash);
        public static bool Has(string hash) => File.Exists(GetPath(hash));

        public static void Move(string old_hash, string new_hash) => File.Move(GetPath(old_hash), GetPath(new_hash));
        public static void Delete(string hash) { if (Has(hash)) File.Delete(GetPath(hash)); }
    }
}