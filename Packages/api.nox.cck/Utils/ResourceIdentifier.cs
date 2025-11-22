using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nox.CCK.Utils {
	[Serializable]
	public class ResourceIdentifier {
		public string group;
		public string[] path;

		public string GetGroup()
			=> group;

		public string[] GetPath()
			=> path;

		public ResourceIdentifier(string group, params string[] path) {
			this.group = group;
			this.path  = path;
		}

		public static ResourceIdentifier Parse(string identifier) {
			if (string.IsNullOrEmpty(identifier))
				throw new ArgumentException("Identifier cannot be null or empty.", nameof(identifier));

			var groupAndPath = identifier.Split(new[] { ':' }, 2);
			var group        = groupAndPath.Length > 1 ? groupAndPath[0] : string.Empty;
			var path = (groupAndPath.Length > 1 ? groupAndPath[1] : groupAndPath[0])
				.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

			return new ResourceIdentifier(group, path);
		}

		public bool HasGroup()
			=> !string.IsNullOrEmpty(group);

		public bool HasPath()
			=> path.Length > 0;

		public override string ToString()
			=> $"{group}{(HasPath() && HasGroup() ? ":" : "")}{string.Join("/", path)}";
	}
}