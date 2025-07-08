// using Nox.CCK.Utils;
// using Nox.Widgets;
// using UnityEngine;
//
// namespace api.nox.user.widget {
// 	public class UserWidgetComportment : MonoBehaviour, IWidgetComportment {
// 		internal UserWidget Widget;
//
// 		public int GetId()
// 			=> GetInstanceID();
//
// 		public string GetFrom()
// 			=> UserWidget.GetDefaultKey();
//
// 		public Vector2Int GetSize()
// 			=> new(1, 1);
//
// 		public void Start() {
// 			Main.OnUserUpdated.AddListener(OnUserUpdated);
// 		}
//
// 		private void OnUserUpdated(MyUser user) {
// 			throw new System.NotImplementedException();
// 		}
//
// 		private void OnClick() { }
//
// 		private void OnDestroy() {
// 			Main.OnUserUpdated.RemoveListener(OnUserUpdated);
// 			Widget = null;
// 		}
// 	}
// }