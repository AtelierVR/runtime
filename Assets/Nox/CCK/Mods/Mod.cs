using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Assets;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Metadata;

namespace Nox.CCK.Mods {
	public interface Mod {
		public ModMetadata GetMetadata();

		public T                          GetData<T>(string key, T defaultValue = default);
		public bool                       SetData<T>(string key, T value);
		public bool                       HasData<T>(string key);
		public Dictionary<string, object> GetDatas();

		public bool          IsLoaded();
		public UniTask<bool> Load();
		public UniTask<bool> Unload();

		public Performance[] GetPerformances();

		public AppDomain GetAppDomain();

		public bool                 IsMainEnabled();
		public MainModInitializer[] GetMains();
		public void                 EnableMain();
		public void                 DisableMain();

		public bool                   IsEditorEnabled();
		public EditorModInitializer[] GetEditors();
		public void                   EnableEditor();
		public void                   DisableEditor();

		public bool                   IsServerEnabled();
		public ServerModInitializer[] GetServers();
		public void                   EnableServer();
		public void                   DisableServer();

		public bool                   IsClientEnabled();
		public ClientModInitializer[] GetClients();
		public void                   EnableClient();
		public void                   DisableClient();

		public bool              IsCustomEnabled(string entry);
		public string[]          GetCustomEntries();
		public IModInitializer[] GetCustom(string       entry);
		public T[]               GetCustom<T>(string    entry) where T : IModInitializer;
		public void              DisableCustom(string   entry);
		public void              EnableCustom<T>(string entry) where T : IModInitializer;
	}
}