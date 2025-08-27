#if UNITY_EDITOR
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.CCK.Build;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.avatar.editor {
	[RequireComponent(typeof(IAvatarDescriptor))]
	public class PlayModeAvatar : MonoBehaviour, IRuntimeAvatar, IRemoveOnBuild {
		public string GetId()
			=> GetInstanceID().ToString();

		public IAvatarDescriptor GetDescriptor()
			=> GetComponent<IAvatarDescriptor>();

		public async UniTask Dispose()
			=> await UniTask.Yield();

		private void Start()
			=> StartAsync().Forget();

		private async UniTask StartAsync() {
			var descriptor = GetComponent<IAvatarDescriptor>();
			if (descriptor == null) {
				Logger.LogError("AvatarDescriptor component missing, destroying avatar.");
				enabled = false;
				return;
			}

			Logger.Log("Avatar starting...");
			if (!await AvatarSetup.Prepare(this)) {
				Logger.LogError("Avatar preparation failed, destroying avatar.");
				enabled = false;
			} else Logger.Log("Avatar prepared successfully.");

			var parameters = descriptor
				.GetModules<IParameterModule>()
				.FirstOrDefault();

			parameters?.GetParameter("IsLocal")?.Set(true);
			parameters?.GetParameter("is_local")?.Set(true);

			parameters?.GetParameter("Grounded")?.Set(true);
			parameters?.GetParameter("grounded")?.Set(true);

			parameters?.GetParameter("Upright")?.Set(1.0f);
			parameters?.GetParameter("upright")?.Set(1.0f);

			parameters?.GetParameter("VRMode")?.Set(0);
			parameters?.GetParameter("use_xr")?.Set(false);

			parameters?.GetParameter("TrackingType")?.Set(3);

			parameters?.GetParameter("tracking/left_hand/active")?.Set(false);
			parameters?.GetParameter("tracking/right_hand/active")?.Set(false);
			parameters?.GetParameter("tracking/head/active")?.Set(false);
			parameters?.GetParameter("tracking/left_foot/active")?.Set(false);
			parameters?.GetParameter("tracking/right_foot/active")?.Set(false);
		}
	}
}
#endif