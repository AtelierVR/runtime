using UnityEngine;
using UnityEngine.Animations;

namespace Nox.CCK.Avatars.Parameters {
	public class PlayableBaseParameter : BaseParameter {
		internal AnimatorControllerPlayable Controller;
		internal Animator                   Animator;

		protected override void SetFloat(float value)
			=> Animator.SetFloat(GetKey(), value);

		protected override void SetInteger(int value)
			=> Animator.SetInteger(GetKey(), value);

		protected override void SetBool(bool value)
			=> Animator.SetBool(GetKey(), value);

		protected override bool GetBool()
			=> Controller.GetBool(GetKey());

		protected override float GetFloat()
			=> Controller.GetFloat(GetKey());

		protected override int GetInteger()
			=> Controller.GetInteger(GetKey());
	}
}