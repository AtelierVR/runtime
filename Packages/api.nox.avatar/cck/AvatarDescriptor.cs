using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars;
using Nox.CCK.Build;
using UnityEngine;
using Transform = UnityEngine.Transform;
using Nox.CCK.Utils;
using Nox.Players;


namespace Nox.CCK.Avatars {
	public class AvatarDescriptor : MonoBehaviour, IAvatarDescriptor, ICompilable {
		public GameObject GetRoot()
			=> gameObject;

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

		public int CompileOrder
			=> 9999;

		// ReSharper disable Unity.PerformanceAnalysis
		public virtual void Compile() {
			if (target == Platform.None)
				target = PlatformExtensions.CurrentPlatform;
			var modules = new List<IAvatarModule>();
			modules.AddRange(GetComponents<IAvatarModule>());
			modules.AddRange(GetComponentsInChildren<IAvatarModule>(true));
			Modules    = modules.ToArray();
			isCompiled = true;
		}
		#endif

		#endregion Build

		#region Animator

		private Animator _animator;

		// ReSharper disable Unity.PerformanceAnalysis
		public Animator GetAnimator() {
			if (!_animator)
				_animator = GetComponent<Animator>();
			return _animator;
		}

		#endregion Animator

		#region Voice

		public Vector3   voicePosition;
		public Transform voiceParent;

		#endregion Voice

		#region Runtime

		private IPlayer _player;

		public void AttachPlayer(IPlayer player)
			=> _player = player;

		public IPlayer GetAttachedPlayer()
			=> _player;

		#endregion

		#region Modules

		public IAvatarModule[] Modules = Array.Empty<IAvatarModule>();

		public T[] GetModules<T>() where T : IAvatarModule
			=> Modules.OfType<T>().ToArray();

		public IAvatarModule[] GetModules()
			=> Modules;

		#endregion Modules
		
		public Vector3 viewPosition = new(0, 1.6f, 0);
	}
}