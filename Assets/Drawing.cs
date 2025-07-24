using UnityEngine;

namespace DefaultNamespace {
	public static class Drawing {
		private static Texture2D lineTex;

		public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width) {
			if (!lineTex) {
				lineTex = new Texture2D(1, 1);
				lineTex.SetPixel(0, 0, Color.white);
				lineTex.Apply();
			}

			Matrix4x4 matrixBackup = GUI.matrix;

			float angle                    = Vector3.Angle(pointB - pointA, Vector2.right);
			if (pointA.y > pointB.y) angle = -angle;
			float length                   = (pointB - pointA).magnitude;

			GUI.color = color;
			GUIUtility.ScaleAroundPivot(new Vector2(length, width), pointA);
			GUIUtility.RotateAroundPivot(angle, pointA);
			GUI.DrawTexture(new Rect(pointA.x, pointA.y, 1, 1), lineTex);
			GUI.matrix = matrixBackup;
		}
	}
}