using UnityEngine;
using UnityEngine.Animations;

namespace Nox.CCK.Avatars.Parameters {
	public class PlayableBaseParameter : BaseParameter {
		internal AnimatorControllerPlayable Controller;
		internal Animator                   Animator;

		protected override void SetFloat(float value)
			=> Animator.SetFloat(GetHash(), value);

		protected override void SetInteger(int value)
			=> Animator.SetInteger(GetHash(), value);

		protected override void SetBool(bool value)
			=> Animator.SetBool(GetHash(), value);

		protected override bool GetBool()
			=> Controller.GetBool(GetHash());

		protected override float GetFloat()
			=> Controller.GetFloat(GetHash());

		protected override int GetInteger()
			=> Controller.GetInteger(GetHash());
	}
}