namespace Nox.Worlds {
	public interface IUpdateWorldRequest {
		/// <summary>
		/// Sets the title of the world.
		/// If the title is empty, no change will be made.
		/// If the title is null, the current title will be removed.
		/// Any other value will set the title to the given value.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public IUpdateWorldRequest SetTitle(string t);

		/// <summary>
		/// Sets the description of the world.
		/// If the description is empty, no change will be made.
		/// If the description is null, the current description will be removed.
		/// Any other value will set the description to the given value.
		/// </summary>
		/// <param name="d"></param>
		/// <returns></returns>
		public IUpdateWorldRequest SetDescription(string d);

		/// <summary>
		/// Sets the capacity of the world.
		/// If the capacity is 0, the capacity will be set as unlimited.
		/// If the capacity is <see cref="ushort.MaxValue"/>, no change will be made.
		/// Any other value will set the capacity to the given value.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public IUpdateWorldRequest SetCapacity(ushort c);

		/// <summary>
		/// Sets the thumbnail for the world.
		/// If the thumbnail is empty, no change will be made.
		/// If the thumbnail is null, the current thumbnail will be removed.
		/// Any other value will set the thumbnail to the given value.
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public IUpdateWorldRequest SetThumbnail(string i);

		/// <summary>
		/// Gets the title of the world.
		/// </summary>
		/// <returns></returns>
		public string GetTitle();

		/// <summary>
		/// Gets the description of the world.
		/// </summary>
		/// <returns></returns>
		public string GetDescription();

		/// <summary>
		/// Gets the capacity of the world.
		/// </summary>
		/// <returns></returns>
		public ushort GetCapacity();

		/// <summary>
		/// Gets the thumbnail of the world.
		/// </summary>
		/// <returns></returns>
		public string GetThumbnail();
	}
}