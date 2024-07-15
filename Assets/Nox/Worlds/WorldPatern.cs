// using System.Collections.Generic;
// using Cysharp.Threading.Tasks;

// namespace Nox.Worlds
// {
//     public class WorldPatern
//     {
//         public string reference;
//         public uint id;
//         public string server;
//         public Dictionary<string, string> tags = new();
//         public ushort Version => ushort.Parse(tags.GetValueOrDefault("v") ?? tags.GetValueOrDefault("version") ?? ushort.MaxValue.ToString());

//         public WorldPatern(string reference, string server = null)
//         {
//             this.reference = reference;
//             var parts = reference.Split('@');
//             this.server = parts.Length > 1 ? (string.IsNullOrEmpty(parts[1]) || parts[1] == "::" ? server : parts[1]) : server;
//             var idParts = parts[0].Split(';');
//             id = uint.Parse(idParts[0]);
//             for (var i = 1; i < idParts.Length; i++)
//             {
//                 var tagParts = idParts[i].Split('=');
//                 tags[tagParts[0]] = string.Join("=", tagParts[1..]);
//             }
//         }

//         public UniTask<World> World() => WorldManager.GetOrFetch(id, server);
        
//         public async UniTask<WorldAsset> Asset() {
//             var w = await World();
//             if (w == null) return null;
//             var a = w.GetAsset(Version);
//             if (a == null) return null;
//             return WorldAsset.From(a);
//         }
//     }
// }