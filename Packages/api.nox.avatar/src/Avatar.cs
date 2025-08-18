using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.CCK.Build;
using UnityEngine;

namespace api.nox.avatar {
	public class Avatar : IAvatar {
		public const string LocalLayer  = "LocalAvatar";
		public const string RemoteLayer = "RemoteAvatar";

		public IAvatarDescriptor Descriptor;

		public static Avatar Setup(IAvatarDescriptor descriptor) {
			var avatar = new Avatar { Descriptor = descriptor, };
			SetupLayers(avatar);
			Build(avatar);
			SetPlay(avatar);
			return avatar;
		}

		private static void SetupLayers(Avatar avatar) {
			var gameObject = avatar.Descriptor.GetRoot();

			if (!gameObject) {
				Debug.LogError("Avatar descriptor is not a MonoBehaviour, cannot set layers.");
				return;
			}

			gameObject.layer = LayerMask.NameToLayer(LocalLayer);
		}

		private static void Build(Avatar avatar) {
			var gameObject = avatar.Descriptor.GetRoot();
			if (!gameObject) return;
			var compilable = gameObject
				.GetComponentsInChildren<ICompilable>(true)
				.OrderBy(c => c.CompileOrder);
			foreach (var c in compilable)
				c.Compile();
		}

		private static void SetPlay(Avatar avatar) {
			var modules = avatar.Descriptor.GetModules();
			foreach (var module in modules)
				module.OnPlay(avatar);
		}

		public string GetId()
			=> Descriptor?.GetRoot()?.GetInstanceID().ToString() ?? "0";

		public IAvatarDescriptor GetDescriptor()
			=> Descriptor;


		public async UniTask Dispose() {
			await UniTask.Yield();
			Descriptor = null;
		}
	}
}