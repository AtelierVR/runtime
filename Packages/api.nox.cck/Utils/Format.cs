namespace Nox.CCK.Utils {
	public static class Format {
		public static string ToSnakeCase(this string input) {
			if (string.IsNullOrEmpty(input))
				return input;

			var sb = new System.Text.StringBuilder();

			var lastWasUnderscore = false;

			for (var i = 0; i < input.Length; i++) {
				var c = input[i];
				if (char.IsUpper(c)) {
					if (i > 0 && !lastWasUnderscore)
						sb.Append('_');
					sb.Append(char.ToLowerInvariant(c));
					lastWasUnderscore = false;
				} else if (char.IsWhiteSpace(c)) {
					if (lastWasUnderscore) continue;
					sb.Append('_');
					lastWasUnderscore = true;
				} else {
					sb.Append(c);
					lastWasUnderscore = false;
				}
			}

			return sb.ToString().Trim('_');
		}
	}
}