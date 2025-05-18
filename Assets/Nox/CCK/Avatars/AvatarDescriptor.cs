using System;
using System.Collections.Generic;
using Nox.CCK.Players;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nox.CCK.Avatars {
	public class AvatarDescriptor : MonoBehaviour {
		#if UNITY_EDITOR
		public string serverPublisher  = null;
		public uint   idPublisher      = 0;
		public ushort versionPublisher = 0;
		#endif

		private Animator _animator;

		public Animator Animator {
			get {
				if (!_animator)
					_animator = GetComponent<Animator>();
				return _animator;
			}
		}

		public Vector3             viewPosition;
		public Vector3             voicePosition;
		public Transform           voiceParent;
		public AvatarMenu          menu;
		public AvatarParameters    parameters;
		public bool                useEyeMovements = false;
		public SkinnedMeshRenderer faceMesh;
		public Vector2Int          eyeIntervalTarget = new(5, 10);
		public EyeLookType         eyeLookType       = EyeLookType.Muscle;
		public EyeLook[]           eyeLooks          = Array.Empty<EyeLook>();

		public EyeLook GetLeftEye()
			=> Array.Find(eyeLooks, e => e.placement == EyePlacement.Left);

		public EyeLook GetRightEye()
			=> Array.Find(eyeLooks, e => e.placement == EyePlacement.Right);


		public static AvatarDescriptor[] GetDescriptors() {
			var descriptors = new List<AvatarDescriptor>();
			for (var i = 0; i < SceneManager.sceneCount; i++)
				if (TryGetDescriptor(SceneManager.GetSceneAt(i), out var descriptor))
					descriptors.Add(descriptor);
			return descriptors.ToArray();
		}

		private static bool TryGetDescriptor(GameObject root, out AvatarDescriptor descriptor) {
			if (root.TryGetComponent(out descriptor))
				return true;

			foreach (Transform child in root.transform)
				if (TryGetDescriptor(child.gameObject, out descriptor))
					return true;

			descriptor = null;
			return false;
		}

		public static bool TryGetDescriptor(Scene scene, out AvatarDescriptor descriptor) {
			if (!scene.IsValid() || !scene.isLoaded) {
				descriptor = null;
				return false;
			}

			foreach (var root in scene.GetRootGameObjects())
				if (TryGetDescriptor(root, out descriptor))
					return true;

			descriptor = null;
			return false;
		}

		[Serializable]
		public class AvatarCollider {
			public AvatarColliderType type = AvatarColliderType.Automatic;
			public HumanBodyBones     rig;
			public Transform          transform;
			public float              radius;
			public float              height;
			public Vector3            offset;
			public Quaternion         rotation;
		}

		public enum AvatarColliderType {
			Automatic,
			Custom
		}

		public enum EyeLookType {
			Muscle,
			Transform,
			BlendShape
		}

		public enum EyePlacement {
			Other,
			Left,
			Right,
			Center
		}

		[Serializable]
		public class EyeLook {
			public EyePlacement placement = EyePlacement.Other;

			public Transform target;
			public Vector4   angleLimits = new(-10, 15, -20, 20);

			public SkinnedMeshRenderer mesh;
			public string[]            blendShapes = { "", "", "", "" };
		}
	}
}