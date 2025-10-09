using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods;

namespace Nox.ModLoader {
	public class Profiler {
		public void Set(string type, At at, DateTime value)
			=> Set(type, null, at, value);

		public void Set(string type, string entry, At at, DateTime value)
			=> Set(type, entry, null, at, value);

		public void Set(string type, string entry, string key, At at, DateTime value) {
			var v = Get(type, entry, key)
				?? new Profile {
					Type  = type,
					Entry = entry,
					Key   = key,
					Start = DateTime.MinValue,
					End   = DateTime.MaxValue
				};
			if (at == At.Start) v.Start = value;
			else v.End                  = value;
			if (Profiles.Contains(v)) Profiles.Remove(v);
			Profiles.Add(v);
		}

		internal readonly List<Profile> Profiles = new();

		public Profile Get(string type, string entry, string key)
			=> Profiles.FirstOrDefault(p => p.Type == type && p.Entry == entry && p.Key == key);


		public enum At {
			Start,
			End
		}
	}
}