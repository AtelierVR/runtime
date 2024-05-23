using System.Collections.Generic;
using UnityAssetBundle = UnityEngine.AssetBundle;

namespace Nox.Assets
{
    public class AssetBundle
    {
        public UnityAssetBundle Value = default;
        public string Id = default;
        public List<string> Tags = new();

        public AssetBundle(string hash, UnityAssetBundle value)
        {
            Id = hash;
            Value = value;
        }

        public override bool Equals(object obj) => obj is AssetBundle asset && Id.Equals(asset.Id);
        public override int GetHashCode() => Id.GetHashCode();
    }
}