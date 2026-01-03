using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using Nox.Players;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nox.Controllers {
	/// <summary>
	/// Provider for movement and interaction with the game.
	/// </summary>
	public interface IController {
		/// <summary>
		/// The default priority for the controller.
		/// </summary>
		const int DefaultPriority = 1;

		/// <summary>
		/// Gets the ID of the controller.
		/// </summary>
		/// <returns></returns>
		public string GetId();

		/// <summary>
		/// Get the priority of the controller.
		/// This is used to determine if another controller should be used instead of this one.
		/// </summary>
		/// <returns></returns>
		public int GetPriority();

		/// <summary>
		/// Get the collider associated with the controller.
		/// </summary>
		/// <returns></returns>
		public Collider GetCollider();

		/// <summary>
		/// Set data for the controller.
		/// </summary>
		/// <param name="controller"></param>
		public UniTask Restore(IController controller);

		/// <summary>
		/// Dispose the controller.
		/// </summary>
		public void Dispose();

		/// <summary>
		/// Get the Camera associated with the controller.
		/// </summary>
		/// <returns></returns>
		public Camera GetCamera();
		
		/// <summary>
		/// Get the EventSystem associated with the controller.
		/// </summary>
		/// <returns></returns>
		public EventSystem GetEventSystem();

		/// <summary>
		/// Get the parts of the controller.
		/// For example, the hands, head, base, etc.
		/// You can use <see cref="HumanBone"/> or <see cref="PlayerRig"/> as ushort index for the parts.
		/// </summary>
		/// <returns></returns>
		public IReadOnlyDictionary<ushort, TransformObject> GetParts();

		/// <summary>
		/// Set a transformation for a part of the controller.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="transform"></param>
		public void SetPart(ushort index, TransformObject transform);

		/// <summary>
		/// Get a part of the controller by index.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="tr"></param>
		/// <returns></returns>
		public bool TryGetPart(ushort index, out TransformObject tr);

		/// <summary>
		/// Get the abilities of the controller.
		/// Is settings for the controller, like movement speed, jump height, etc.
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, object> GetAbilities();

		/// <summary>
		/// Set an ability for the controller.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void SetAbilities(string key, object value);
	}
}