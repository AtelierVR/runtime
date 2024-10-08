using System;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.test
{
    public class Widget : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string id;
        [ShareObjectImport, ShareObjectExport] public uint width = 1;
        [ShareObjectImport, ShareObjectExport] public uint height = 1;
        [ShareObjectImport, ShareObjectExport] public Func<int, Transform, GameObject> GetContent;
        [ShareObjectImport, ShareObjectExport] public bool isInteractable = true;
        [ShareObjectImport, ShareObjectExport] public uint weight = 1;
    }
}