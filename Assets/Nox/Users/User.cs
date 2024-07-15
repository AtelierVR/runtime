// using System;
// using Cysharp.Threading.Tasks;
// using Nox.Servers;

// namespace Nox.Users
// {
//     [Serializable]
//     public class User
//     {
//         public uint id;
//         public string username;
//         public string display;
//         public string server;
//         public string[] tags;
//         public string[] links;
//         public float rank;

//         public bool IsMe = false;
//         public UniTask<User> Update() => UserManager.Fetch(id, server);
//         public UniTask<Server> Server() => ServerManager.GetOrFetch(server);
//     }
// }