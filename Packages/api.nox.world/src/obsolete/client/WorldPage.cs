// using System;
// using System.Collections.Generic;
// using Cysharp.Threading.Tasks;
// using Nox.CCK.Mods.Events;
// using Nox.CCK.Utils;
// using UnityEngine;
// using Object = UnityEngine.Object;
// using Transform = UnityEngine.Transform;
//
// namespace api.nox.world.client
// {
//     public class WorldPage
//     {
//         public static string GetKey() => "world";
//         private static EventSubscription _listener;
//
//         public static void Listen()
//         {
//             _listener = Main.CoreAPI.EventAPI.Subscribe("goto_page", OnGotoEvent);
//         }
//
//         public static void StopListen()
//         {
//             Main.CoreAPI.EventAPI.Unsubscribe(_listener);
//         }
//         
//         
//         private static void OnGotoEvent(EventData context)
//         {
//             if (!context.TryGet(0, out int menuId)) return;
//             if (!context.TryGet(1, out string pageKey)) return;
//             if (!context.TryGet(2, out string type)) return;
//             if (pageKey != GetKey()) return;
//             switch (type)
//             {
//                 case "id-server" when context.TryGet(3, out string id0) && context.TryGet(4, out uint ser0):
//                     OnPageByIdentifier(menuId, Main.WorldAPI.CallMethod("IdentifierById", id0, ser0));
//                     break;
//                 case "identifier" when context.TryGet(3, out string id2):
//                     OnPageByIdentifier(menuId, Main.WorldAPI.CallMethod("IdentifierByString", id2));
//                     break;
//                 case "world" when context.TryGet(3, out INoxObject usr3):
//                     OnPageByWorld(menuId, usr3);
//                     break;
//                 default:
//                     context.Callback("Invalid page type");
//                     break;
//             }
//         }
//         
//         private static void OnPageByIdentifier(int menuId, INoxObject identifier)
//         {
//             var page = new WorldPage
//             {
//                 MenuId = menuId,
//                 Identifier = identifier,
//                 World = Main.WorldAPI.CallMethod("GetWorldByIdentifier", identifier)
//             };
//             page.Display();
//             page.Refresh().Forget();
//         }
//         
//         private static void OnPageByWorld(int menuId, INoxObject world)
//         {
//             var page = new WorldPage
//             {
//                 MenuId = menuId,
//                 Identifier = world.CallMethod("ToIdentifier"),
//                 World = world
//             };
//             page.Display();
//         }
//         
//         private void Display()
//             => Main.CoreAPI.EventAPI.Emit("display_page", MenuId, new Dictionary<string, object>
//             {
//                 {
//                     "key", GetKey()
//                 }, // id of the page
//                 {
//                     "content", new Func<Transform, GameObject>(OnContent)
//                 }, // called when the menu need the content of the page (first call)
//                 /*
//                  {
//                      "open", (string key, GameObject go) => OnOpen(key, go)
//                  }, // called once when the page is display for the first time
//                  {
//                      "restore", (string key, GameObject go) => OnRestore(key, go)
//                  }, // called when the menu go back from history and display the page again
//                  {
//                      "remove", (GameObject go) => OnRemove(go)
//                  }, // called when the menu remove the page from history (last call)
//                  {
//                      "display", (string key, GameObject go) => OnDisplay(key, go)
//                  }, // called when the page is displayed
//                  {
//                      "hide", (string key, GameObject go) => OnHide(key, go)
//                  } // called when another page is displayed
//                  */
//             });
//
//         internal int MenuId;
//         internal INoxObject Identifier;
//         internal INoxObject World;
//         internal bool IsFetching;
//         private WorldComportment _comportment;
//         
//         private GameObject OnContent(Transform transform)
//         {
//             var asset = Main.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/content.prefab");
//             asset.SetActive(false);
//             var content = Object.Instantiate(asset, transform);
//             _comportment = content.GetComponent<WorldComportment>();
//             _comportment.Initiate(this);
//             _comportment.UpdateData();
//             content.name = $"{GetKey()}_{content.name}";
//             return content;
//         }
//         
//         internal void GoBack()
//             => Main.CoreAPI.EventAPI.Emit("goto_action", MenuId, "back");
//
//         internal async UniTask Refresh()
//         {
//             if (IsFetching || World == null) return;
//             IsFetching = true;
//             _comportment.UpdateData();
//             await World.InvokeAsyncMethod("Refresh");
//             IsFetching = false;
//             _comportment.UpdateData();
//         }
//
//     }
// }