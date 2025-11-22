using System;

namespace Nox.CCK.Utils {
	public class ResourceIdentifier {
		private readonly string   _group;
		private readonly string[] _path;

		public string GetGroup()
			=> _group;

		public string[] GetPath()
			=> _path;

		public ResourceIdentifier(string group, params string[] path) {
			_group = group;
			_path  = path;
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
			=> !string.IsNullOrEmpty(_group);

		public bool HasPath()
			=> _path.Length > 0;

		public override string ToString()
			=> $"{_group}{(HasPath() && HasGroup() ? ":" : "")}{string.Join("/", _path)}";
	}
}