
// using Cysharp.Threading.Tasks;
// using Nox.Worlds;

// namespace Nox.Instances
// {
//     public class Instance
//     {
//         public uint id;
//         public string description;
//         public string name;
//         public ushort capacity;
//         public string server;
//         public string[] tags;
//         public string world_ref;

//         public async UniTask<bool> Update() => await InstanceManager.Fetch(id, server) != null;

//         public async UniTask<World> World() => await WorldManager.GetOrFetch(world_ref);
//     }
// }