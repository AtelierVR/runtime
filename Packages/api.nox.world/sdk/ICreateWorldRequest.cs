namespace Nox.Worlds {
	public interface ICreateWorldRequest {
		/// <summary>
		/// Sets the ID of the world to create.
		/// If the ID is 0, a new ID will be generated.
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public ICreateWorldRequest SetId(uint i);

		/// <summary>
		/// Sets the title of the world to create.
		/// If the title is null or empty, a default title will be used.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public ICreateWorldRequest SetTitle(string t);

		/// <summary>
		/// Sets the description of the world to create.
		/// If the description is null or empty, no description will be set.
		/// </summary>
		/// <param name="d"></param>
		/// <returns></returns>
		public ICreateWorldRequest SetDescription(string d);

		/// <summary>
		/// Sets the capacity of the world to create.
		/// If the capacity is 0, the capacity will be defaulted to unlimited.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public ICreateWorldRequest SetCapacity(ushort c);

		/// <summary>
		/// Sets the thumbnail of the world to create.
		/// If the thumbnail is null or empty, no thumbnail will be set.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public ICreateWorldRequest SetThumbnail(string t);

		/// <summary>
		/// Get the ID of the world to create.
		/// </summary>
		/// <returns></returns>
		public uint GetId();

		/// <summary>
		/// Gets the title of the world to create.
		/// </summary>
		/// <returns></returns>
		public string GetTitle();
		
		/// <summary>
		/// Gets the description of the world to create.
		/// </summary>
		/// <returns></returns>
		public string GetDescription();
		
		/// <summary>
		/// Gets the capacity of the world to create.
		/// </summary>
		/// <returns></returns>
		public ushort GetCapacity();
		
		/// <summary>
		/// Gets the thumbnail of the world to create.
		/// </summary>
		/// <returns></returns>
		public string GetThumbnail();
	}
}