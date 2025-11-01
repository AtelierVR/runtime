using System.Linq;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Parameters {
	public class AnimatorBaseParameter : BaseParameter {
		internal Animator Animator;

		protected override void SetFloat(float value)
			=> Animator.SetFloat(GetHash(), value);

		protected override void SetInteger(int value)
			=> Animator.SetInteger(GetHash(), value);

		protected override void SetBool(bool value)
			=> Animator.SetBool(GetHash(), value);

		protected override float GetFloat()
			=> Animator.GetFloat(GetHash());

		protected override int GetInteger()
			=> Animator.GetInteger(GetHash());

		protected override bool GetBool()
			=> Animator.GetBool(GetHash());
	}
}