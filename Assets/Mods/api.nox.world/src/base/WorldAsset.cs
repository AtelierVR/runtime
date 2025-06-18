using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.world {
	[Serializable]
	public class WorldAsset : INoxObject {
		public uint     id;
		public uint     version;
		public string   engine;
		public string   platform;
		public bool?    is_empty;
		public string   url;
		public string   hash;
		public uint?    size;
		public string[] mods;
		public string[] features;

		public uint GetId()
			=> id;

		public uint GetVersion()
			=> version;

		public string GetEngine()
			=> engine;

		public string GetPlatform()
			=> platform;

		public bool IsEmpty()
			=> is_empty ?? false;

		public string GetUrl()
			=> url;

		public string GetHash()
			=> hash;

		public uint? GetSize()
			=> size;

		public string[] GetMods()
			=> mods ?? Array.Empty<string>();

		public string[] GetFeatures()
			=> features ?? Array.Empty<string>();
	}
}
