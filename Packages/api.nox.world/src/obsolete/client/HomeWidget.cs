// using System;
// using System.Collections.Generic;
// using Nox.CCK.Utils;
// using UnityEngine;
// using Object = UnityEngine.Object;
//
// namespace api.nox.world
// {
//     public class HomeWidget : IDisposable
//     {
//         private Dictionary<string, object> _widget;
//
//         private INoxObject WidgetAPI
//             => WorldClient.UISystem
//                 .GetField("Widgets");
//
//         internal HomeWidget()
//         {
//             WorldClient.Instance.OnHomeUpdated.AddListener(OnHomeUpdated);
//         }
//         
//         public void Dispose()
//         {
//             WorldClient.Instance.OnHomeUpdated.RemoveListener(OnHomeUpdated);
//         }
//
//         private void OnHomeUpdated(INoxObject home)
//         {
//             if (WorldClient.UISystem == null) return;
//
//             _widget ??= new Dictionary<string, object>
//             {
//                 { "key", "home" },
//                 { "width", 1 },
//                 { "height", 1 }
//             };
//
//             if (home == null && WidgetAPI.CallMethod<bool>("Has", _widget["key"]))
//                 WidgetAPI.CallMethod("Remove", _widget["key"]);
//
//             if (home == null) return;
//
//             _widget["content"] = new Func<RectTransform, GameObject>(rect =>
//             {
//                 var asset = Main.CoreAPI.AssetAPI.GetAsset<GameObject>("ui", "prefabs/widgets/button.prefab");
//                 var button = Object.Instantiate(asset, rect);
//                 var reference = Reference.GetReference("content", button);
//                 asset = Main.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/widget.prefab");
//                 var widget = Object.Instantiate(asset, reference.transform);
//                 var comportment = widget.GetComponent<HomeWidgetComportment>();
//                 comportment.button = button.GetComponent<UnityEngine.UI.Button>();
//                 comportment.UpdateContent(home);
//                 return button;
//             });
//
//             WidgetAPI.CallMethod(
//                 WidgetAPI.CallMethod<bool>("Has", _widget["key"])
//                     ? "Change"
//                     : "Add",
//                 _widget
//             );
//         }
//     }
// }