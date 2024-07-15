// using System;
// using Cysharp.Threading.Tasks;
// using Nox.Worlds;

// namespace Nox.Users
// {
//     [Serializable]
//     public class UserMe : User
//     {
//         public string home;

//         public UniTask<World> Home() => WorldManager.GetOrFetch(home, server);
//         public WorldPatern HomePatern() => new(home, server);
//     }
// }