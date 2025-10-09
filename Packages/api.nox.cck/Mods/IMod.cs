using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Assets;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Metadata;

namespace Nox.CCK.Mods {
	public interface IMod {
		public ModMetadata GetMetadata();

		public T GetData<T>(string key, T defaultValue = default);

		public bool SetData<T>(string key, T value);

		public bool HasData<T>(string key);

		public Dictionary<string, object> GetDatas();

		public bool IsLoaded();

		public UniTask<bool> Load();

		public UniTask<bool> Unload();

		public Profile[] GetProfiler();

		public AppDomain GetAppDomain();

		public T GetInstance<T>();

		public T[] GetInstances<T>();
	}
}