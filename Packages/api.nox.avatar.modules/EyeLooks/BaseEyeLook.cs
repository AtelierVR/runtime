using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.UIElements;
#endif

namespace Nox.CCK.Avatars.EyeLooks {
	[Serializable]
	public abstract class BaseEyeLook {
		public Vector4 limits = new(-10, 15, -20, 20);

		public virtual Vector4 Limit(EyeLookContext context, Vector2 direction) {
			var left = direction.x < limits.z
				? -limits.z
				: direction.x < 0
					? -direction.x
					: 0;

			var right = direction.x > limits.w
				? limits.w
				: direction.x > 0
					? direction.x
					: 0;

			var up = direction.y > limits.y
				? limits.y
				: direction.y > 0
					? direction.y
					: 0;

			var down = direction.y < limits.x
				? -limits.x
				: direction.y < 0
					? -direction.y
					: 0;

			return new Vector4(down, up, left, right);
		}


		public virtual void UpdateEye(EyeLookContext context, Vector2 direction)
			=> UpdateEye(context, Limit(context, direction));

		public abstract void UpdateEye(EyeLookContext context, Vector4 direction);

		#if UNITY_EDITOR
		public virtual VisualElement CreateInspectorGUI(EyeLookContext context)
			=> new Label("Base Eye Look Inspector");
		#endif
	}
}