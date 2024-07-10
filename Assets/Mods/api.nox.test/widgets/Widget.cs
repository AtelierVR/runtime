using System;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.test {
    public class Widget : ShareObject
    {
        public string id;
        public uint width = 1;
        public uint height = 1;
        public Func<Transform, GameObject> GetContent;
        public bool isInteractable = true;
        public uint weight = 1;
    }
}