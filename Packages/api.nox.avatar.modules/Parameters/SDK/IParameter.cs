using Nox.CCK.Network;
using UnityEngine.Events;

namespace Nox.Avatars.Parameters {
	public interface IParameter {
		/// <summary>
		/// Gets the name of the parameter.
		/// </summary>
		/// <returns></returns>
		public string GetName();

		/// <summary>
		/// Gets the unique identifier of the parameter.
		/// </summary>
		/// <returns></returns>
		public int GetKey();

		/// <summary>
		/// Gets the flags associated with the parameter.
		/// </summary>
		/// <returns></returns>
		public ParameterFlags GetFlags();

		/// <summary>
		/// Gets the type of the parameter value.
		/// </summary>
		/// <returns></returns>
		public ParameterType GetValueType();

		/// <summary>
		/// Gets the value of the parameter as an object.
		/// This method should be used with caution as it returns a generic object type.
		/// </summary>
		/// <returns></returns>
		public object Get();

		/// <summary>
		/// Sets the value of the parameter.
		/// Verify the type of the value before calling this method to ensure it matches the parameter's type.
		/// </summary>
		/// <param name="value"></param>
		public void Set(object value);
	}
}