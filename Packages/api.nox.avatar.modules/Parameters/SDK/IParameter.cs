namespace Nox.Avatars.Parameters {
	public interface IParameter {
		/// <summary>
		/// Gets the name of the parameter.
		/// </summary>
		/// <returns></returns>
		public string GetName();

		/// <summary>
		/// Checks if the parameter is valid.
		/// </summary>
		/// <returns></returns>
		public bool IsValid();
		
		/// <summary>
		/// Gets the unique identifier of the parameter.
		/// </summary>
		/// <returns></returns>
		public int GetHash();

		/// <summary>
		/// Checks if the parameter is syncable with networks.
		/// </summary>
		/// <returns></returns>
		public bool IsSyncable();

		/// <summary>
		/// Checks if the parameter can be saved.
		/// Or reset to default when the avatar is (re)initialized.
		/// </summary>
		/// <returns></returns>
		public bool IsSavable();

		/// <summary>
		/// Checks if the parameter is read-only.
		/// </summary>
		/// <returns></returns>
		public bool IsReadOnly();

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

		public byte[] Serialize();

		public void Deserialize(byte[] data);
	}
}