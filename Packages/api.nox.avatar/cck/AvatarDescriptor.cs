using System;
using Nox.Avatars;
using Nox.CCK.Build;
using UnityEngine;
using Transform = UnityEngine.Transform;
using Nox.CCK.Utils;
using Nox.Players;


namespace Nox.CCK.Avatars {
	public class AvatarDescriptor : MonoBehaviour, IAvatarDescriptor, ICompilable {
		#region Publisher

		#if UNITY_EDITOR
		public Platform target;
		public uint     publishId;
		public string   publishServer;
		public uint     publishVersion;
		#endif

		#endregion

		#region Build

		#if UNITY_EDITOR
		public bool isCompiled;

		public virtual void Compile() {
			if (target == Platform.None)
				target = PlatformExtensions.CurrentPlatform;
			isCompiled = true;
		}
		#endif

		#endregion Build

		#region Animator

		private Animator _animator;

		public Animator Animator {
			get {
				if (!_animator)
					_animator = GetComponent<Animator>();
				return _animator;
			}
		}

		#endregion Animator

		#region Eyes

		public bool                useEyeMovements   = false;
		public Vector3             viewPosition      = new(0, 1.6f, 0);
		public Vector2Int          eyeIntervalTarget = new(5, 10);
		public EyeLookType         eyeLookType       = EyeLookType.Transform;
		public EyeLook[]           eyeLooks          = Array.Empty<EyeLook>();
		public SkinnedMeshRenderer faceMesh;

		public EyeLook GetLeftEye()
			=> Array.Find(eyeLooks, e => e.placement == EyePlacement.Left);

		public EyeLook GetRightEye()
			=> Array.Find(eyeLooks, e => e.placement == EyePlacement.Right);

		public EyeLook[] GetEyes(EyePlacement placement)
			=> Array.FindAll(eyeLooks, e => e.placement == placement);

		public EyeLook[] GetEyes()
			=> eyeLooks;

		#endregion Eyes

		#region Voice

		public Vector3   voicePosition;
		public Transform voiceParent;

		#endregion Voice

		#region Runtime

		private IPlayer _player;

		public void SetPlayer(IPlayer player)
			=> _player = player;

		public IPlayer GetPlayer()
			=> _player;

		#region Modules

		private readonly IAvatarModule[] _modules = Array.Empty<IAvatarModule>();

		public IAvatarModule[] GetModules<T>() where T : IAvatarModule
			=> Array.FindAll(_modules, m => m is T);

		public IAvatarModule[] GetModules()
			=> _modules;

		#endregion Modules

		#endregion

		public AvatarMenu       menu;
		public AvatarParameters parameters;
	}
}