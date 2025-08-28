using UnityEngine;

namespace Nox.CCK.Utils {
	public static class CameraHelper {
		public static Vector4 SpacePlane(Matrix4x4 worldToCameraMatrix, Vector3 pos, Vector3 normal, float clipPlaneOffset) {
			var offsetPos = pos + normal * clipPlaneOffset;
			var cPos      = worldToCameraMatrix.MultiplyPoint(offsetPos);
			var cNormal   = worldToCameraMatrix.MultiplyVector(normal).normalized;
			return new Vector4(cNormal.x, cNormal.y, cNormal.z, -Vector3.Dot(cPos, cNormal));
		}
	}
}