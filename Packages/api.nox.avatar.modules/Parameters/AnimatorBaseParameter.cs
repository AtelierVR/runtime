using System.Linq;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Parameters {
	public class AnimatorBaseParameter : BaseParameter {
		internal Animator Animator;

		protected override void SetFloat(float value)
			=> Animator.SetFloat(GetKey(), value);

		protected override void SetInteger(int value)
			=> Animator.SetInteger(GetKey(), value);

		protected override void SetBool(bool value)
			=> Animator.SetBool(GetKey(), value);

		protected override float GetFloat()
			=> Animator.GetFloat(GetKey());

		protected override int GetInteger()
			=> Animator.GetInteger(GetKey());

		protected override bool GetBool()
			=> Animator.GetBool(GetKey());
	}
}