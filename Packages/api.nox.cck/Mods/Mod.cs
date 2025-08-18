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

		public T GetEntry<T>();

		public bool                 IsMainEnabled();
		public MainModInitializer[] GetMains();
		public T                    GetMain<T>();
		public void                 EnableMain();
		public void                 DisableMain();

		public bool                   IsEditorEnabled();
		public EditorModInitializer[] GetEditors();
		public T                      GetEditor<T>();
		public void                   EnableEditor();
		public void                   DisableEditor();

		public bool                   IsServerEnabled();
		public ServerModInitializer[] GetServers();
		public T                      GetServer<T>();
		public void                   EnableServer();
		public void                   DisableServer();

		public bool                   IsClientEnabled();
		public ClientModInitializer[] GetClients();
		public T                      GetClient<T>();
		public void                   EnableClient();
		public void                   DisableClient();

		public bool              IsCustomsEnabled(string entry);
		public string[]          GetCustomsEntries();
		public IModInitializer[] GetCustoms(string       entry);
		public T[]               GetCustoms<T>(string    entry) where T : IModInitializer;
		public T                 GetCustom<T>(string     entry);
		public void              DisableCustoms(string   entry);
		public void              EnableCustoms<T>(string entry) where T : IModInitializer;
	}
}