using Nox.CCK.Mods.Chats;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Mods;
using Nox.CCK.Mods.Assets;
using Nox.CCK.Mods.Configs;
using Nox.CCK.Mods.Loggers;
using Nox.CCK.Mods.Metadata;

namespace Nox.CCK.Mods.Cores {
	/// <summary>
	/// Main Core API interface for mods, providing access to various system APIs.
	/// </summary>
	public interface IModCoreAPI {
		/// <summary>
		/// Gets the metadata of the current mod.
		/// </summary>
		public ModMetadata ModMetadata { get; }

		/// <summary>
		/// Gets the chat management API.
		/// </summary>
		public IChatAPI ChatAPI { get; }

		/// <summary>
		/// Gets the event management API.
		/// </summary>
		public IEventAPI EventAPI { get; }

		/// <summary>
		/// Gets the mod management API.
		/// </summary>
		public IModAPI ModAPI { get; }

		/// <summary>
		/// Gets the asset management API.
		/// </summary>
		public IAssetAPI AssetAPI { get; }

		/// <summary>
		/// Gets the configuration management API.
		/// </summary>
		public IConfigAPI ConfigAPI { get; }

		/// <summary>
		/// Gets the logger API.
		/// </summary>
		public ILoggerAPI LoggerAPI { get; }
	}
}