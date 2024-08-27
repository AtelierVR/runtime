using Nox.CCK.Mods;
using UnityEngine;

namespace Nox.SimplyLibs
{
    public class SimplyTransform : ShareObject
    {
        [ShareObjectExport, ShareObjectImport] public Vector3 position;
        [ShareObjectExport, ShareObjectImport] public Quaternion rotation;
        [ShareObjectExport, ShareObjectImport] public Vector3 scale;

        [ShareObjectExport, ShareObjectImport] public Vector3 velocity = Vector3.zero;
        [ShareObjectExport, ShareObjectImport] public Vector3 angularVelocity = Vector3.zero;
    }
}