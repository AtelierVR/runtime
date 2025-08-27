using Nox.Avatars;
using Nox.Avatars.StateMachines;
using UnityEngine;

namespace Nox.CCK.Avatars.StateMachines {
	public abstract class BaseStateMachine : StateMachineBehaviour, IStateMachine {
		protected IRuntimeAvatar RuntimeAvatar;
		public virtual bool Setup(IRuntimeAvatar runtime) {
			RuntimeAvatar = runtime;
			return true;
		}
	}
}