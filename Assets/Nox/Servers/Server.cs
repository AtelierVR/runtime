// using System;
// using Cysharp.Threading.Tasks;

// namespace Nox.Servers
// {
//     [Serializable]
//     public class Server
//     {
//         public string id;
//         public string address;
//         public string title;
//         public string description;
//         public string version;
//         public string icon;
//         public ServerGateways gateways;
//         public ulong ready_at;
//         public string public_key;

//         public UniTask<Server> Update() => ServerManager.Fetch(id);
//     }
// }