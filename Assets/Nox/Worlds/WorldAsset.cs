// using System;
// using Cysharp.Threading.Tasks;

// namespace Nox.Worlds
// {
//     [Serializable]
//     public class WorldAsset : CCK.Worlds.Asset
//     {
//         public UniTask<bool> Download() => WorldManager.DownloadAsset(this);

//         public static WorldAsset From(CCK.Worlds.Asset asset) => new()
//         {
//             hash = asset.hash,
//             url = asset.url,
//             size = asset.size,
//             version = asset.version,
//             engine = asset.engine,
//             platform = asset.platform,
//             is_empty = asset.is_empty,
//             id = asset.id
//         };
//     }
// }