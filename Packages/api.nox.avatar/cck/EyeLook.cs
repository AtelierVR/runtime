using System;
using UnityEngine;

namespace Nox.CCK.Avatars {
	[Serializable]
	public class EyeLook {
		public EyePlacement placement = EyePlacement.Other;

		public Transform target;
		public Vector4   angleLimits = new(-10, 15, -20, 20);

		public SkinnedMeshRenderer mesh;
		public string[]            blendShapes = { "", "", "", "" };
	}
}