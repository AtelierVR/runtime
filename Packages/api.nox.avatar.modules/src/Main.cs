using System;
using Nox.Avatars;
using Nox.CCK.Avatars.Camera;
using Nox.CCK.Avatars.EyeLooks;
using Nox.CCK.Avatars.Parameters;
using Nox.CCK.Avatars.Playable;
using Nox.CCK.Avatars.Rigging;
using Nox.CCK.Avatars.Voice;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using UnityEngine;

namespace api.nox.avatar.modules {
	public class Main : IMainModInitializer {
		internal static MainModCoreAPI      CoreAPI;
		private         EventSubscription[] _events;

		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI = api;
			_events = new[] {
				api.EventAPI.Subscribe("avatar_check_request", OnCheckRequest),
			};
			PlayableAvatarModule.GetAssetController = () 
				=> CoreAPI.AssetAPI.GetAsset<RuntimeAnimatorController>("avatar", "animations/Default.controller");
		}

		private static void OnCheckRequest(EventData context) {
			if (!context.TryGet<IAvatarDescriptor>(0, out var descriptor))
				return;
			var valid = true;
			valid &= CameraAvatarModule.Check(descriptor);
			valid &= AvatarParameterModule.Check(descriptor);
			valid &= PlayableAvatarModule.Check(descriptor);
			valid &= BaseRiggingModule.Check(descriptor);
			valid &= EyeLookAvatarModule.Check(descriptor);
			valid &= VoiceAvatarModule.Check(descriptor);
			context.Callback(valid);
		}

		public void OnDisposeMain() {
			foreach (var e in _events)
				CoreAPI.EventAPI.Unsubscribe(e);

			_events = Array.Empty<EventSubscription>();

			PlayableAvatarModule.GetAssetController = null;

			CoreAPI = null;
		}
	}
}