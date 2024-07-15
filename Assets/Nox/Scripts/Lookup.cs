// using System.Threading.Tasks;
// using Cysharp.Threading.Tasks;
// using Nox.CCK;
// using Nox.Servers;
// using Nox.Users;

// namespace Nox.Scripts
// {
//     public class Lookup
//     {
//         public static string MostAuth(string server)
//         {
//             var config = Config.Load();
//             var userme = UserManager.CurrentUser();
//             if (userme != null && userme.server == server && config.Has("token"))
//                 return $"Bearer {config.Get<string>("token")}";
//             return null;
//         }
//     }
// }