// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Nox.CCK.Utils;
// using Nox.Widgets;
// using UnityEngine;
// using Logger = Nox.CCK.Utils.Logger;
// using Object = UnityEngine.Object;
//
// namespace api.nox.user.widget {
// 	public class UserWidget : IWidget {
// 		private List<UserWidgetComportment> _widget = new();
//
// 		public static string GetDefaultKey()
// 			=> "my_user";
//
// 		private static IWidgetAPI WidgetAPI
// 			=> Main.CoreAPI.ModAPI.GetMod("widget")
// 				.GetClients()
// 				.FirstOrDefault() as IWidgetAPI;
//
// 		public string GetKey()
// 			=> GetDefaultKey();
//
// 		public GameObject Build(RectTransform parent) {
// 			var asset       = Main.CoreAPI.AssetAPI.GetAsset<GameObject>("widget", "button_icon.prefab");
// 			var button      = Object.Instantiate(asset, parent);
// 			var comportment = button.AddComponent<UserWidgetComportment>();
// 			comportment.Widget = this;
// 			button.name        = $"[{GetKey()}_{comportment.GetId()}]";
// 			_widget.Add(comportment);
// 			return button;
// 		}
//
//
// 		internal UserWidget() {
// 			WidgetAPI.Add(this);
// 		}
//
// 		public void Dispose() {
// 			WidgetAPI.Remove(GetKey());
// 		}
//
// 		private void OnUserUpdated(INoxObject user) {
// 			if (Client.UISystem == null) {
// 				Logger.LogDebug($"UISystem is null");
// 				return;
// 			}
//
// 			_widget ??= new Dictionary<string, object> {
// 				{ "key", "my_user" },
// 				{ "width", 1 },
// 				{ "height", 1 },
// 				{ "content", null }
// 			};
//
// 			if (user == null && WidgetAPI.CallMethod<bool>("Has", _widget["key"]))
// 				WidgetAPI.CallMethod("Remove", _widget["key"]);
//
// 			if (user == null) {
// 				Logger.LogDebug($"User is null");
// 				return;
// 			}
//
// 			_widget["content"] = new Func<int, RectTransform, GameObject>(OnContent);
//
// 			WidgetAPI.CallMethod(
// 				WidgetAPI.CallMethod<bool>("Has", _widget["key"])
// 					? "Change"
// 					: "Add",
// 				_widget
// 			);
// 		}
//
// 		private GameObject OnContent(int menuId, RectTransform transform) {
// 			var asset     = Main.CoreAPI.AssetAPI.GetAsset<GameObject>("ui", "prefabs/widgets/button.prefab");
// 			var button    = Object.Instantiate(asset, transform);
// 			var reference = Reference.GetReference("content", button);
// 			asset = Main.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/widget.prefab");
// 			var widget      = Object.Instantiate(asset, reference.transform);
// 			var comportment = widget.GetComponent<UserWidgetComportment>();
// 			comportment.button = button.GetComponent<UnityEngine.UI.Button>();
// 			comportment.menuId = menuId;
// 			comportment.UpdateContent(Main.UserAPI.CallMethod("GetCurrentUser"));
// 			return button;
// 		}
// 	}
// }