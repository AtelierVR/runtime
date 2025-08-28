using Nox.CCK.Mods.Chats;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Mods;
using Nox.CCK.Mods.Assets;
using Nox.CCK.Mods.Configs;
using Nox.CCK.Mods.Loggers;
using Nox.CCK.Mods.Metadata;

namespace Nox.CCK.Mods.Cores {
	public interface IModCoreAPI {
		public ModMetadata ModMetadata { get; }

		public IChatAPI ChatAPI { get; }

		public IEventAPI EventAPI { get; }

		public IModAPI ModAPI { get; }

		public IAssetAPI AssetAPI { get; }

		public IConfigAPI ConfigAPI { get; }

		public ILoggerAPI LoggerAPI { get; }
	}
}