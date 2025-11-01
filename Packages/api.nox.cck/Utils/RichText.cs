using System.Text;
using System.Text.RegularExpressions;
using Nox.CCK.Language;

namespace Nox.CCK.Utils {
	public static class RichText {
		/// <summary>
		/// Convertit le texte Markdown en RichText TextMeshPro et l'applique au composant
		/// </summary>
		public static void SetMarkdown(this TMPro.TextMeshProUGUI text, string markdown)
			=> text.text = MarkdownToRichText(markdown);

		public static void SetMarkdown(this TextLanguage text, string markdown)
			=> text.UpdateText("value", new[] { MarkdownToRichText(markdown) });

		/// <summary>
		/// Convertit du Markdown en format RichText compatible avec TextMeshPro
		/// </summary>
		/// <param name="markdown">Le texte Markdown à convertir</param>
		/// <returns>Le texte au format RichText TextMeshPro</returns>
		public static string MarkdownToRichText(string markdown) {
			if (string.IsNullOrEmpty(markdown))
				return string.Empty;

			var result = new StringBuilder(markdown);

			// Blocs de code multilignes (```lang\ncode\n```) - À traiter en PREMIER pour éviter les conflits
			result = ConvertPattern(result, @"```(?:[a-zA-Z#+-]+\r?\n)?([\s\S]*?)```", "<color=#A0A0A0>$1</color>", RegexOptions.Singleline);

			// Code inline (`code`)
			result = ConvertPattern(result, @"`([^`\n]+?)`", "<color=#A0A0A0>$1</color>");

			// Titres (# ## ###) - Avant les couleurs pour éviter confusion avec #RRGGBB
			result = ConvertHeaders(result);

			// Gras + Italique (***texte*** ou ___texte___) - Avant gras et italique simples
			result = ConvertPattern(result, @"\*\*\*(.+?)\*\*\*", "<b><i>$1</i></b>");
			result = ConvertPattern(result, @"___(.+?)___", "<b><i>$1</i></b>");

			// Gras (**texte** ou __texte__)
			result = ConvertPattern(result, @"\*\*(.+?)\*\*", "<b>$1</b>");
			result = ConvertPattern(result, @"__(.+?)__", "<b>$1</b>");

			// Italique (*texte* ou _texte_)
			result = ConvertPattern(result, @"\*([^\*\n]+?)\*", "<i>$1</i>");
			result = ConvertPattern(result, @"(?<!_)_([^_\n]+?)_(?!_)", "<i>$1</i>");

			// Barré (~~texte~~)
			result = ConvertPattern(result, @"~~(.+?)~~", "<s>$1</s>");

			// Liens [texte](url) - affiche juste le texte avec une couleur
			result = ConvertPattern(result, @"\[(.+?)\]\((.+?)\)", "<color=#4A9EFF><u>$1</u></color>");

			// Blockquotes (> texte) - Accepte > avec ou sans espace
			result = ConvertPattern(result, @"^>\s*(.+)$", "<color=#808080>▌ <i>$1</i></color>", RegexOptions.Multiline);

			// Couleurs (#RRGGBB texte) - En dernier et uniquement si pas dans une balise existante
			result = ConvertPattern(result, @"(?<!mark=)(?<!color=)#([0-9A-Fa-f]{6})(?!>)\s+(.+?)(?=\n|$)", "<color=#$1>$2</color>");

			return result.ToString();
		}

		private static StringBuilder ConvertHeaders(StringBuilder text) {
			// H1 (# Titre)
			text = ConvertPattern(text, @"^#\s+(.+)$", "<size=200%><b>$1</b></size>", RegexOptions.Multiline);

			// H2 (## Titre)
			text = ConvertPattern(text, @"^##\s+(.+)$", "<size=175%><b>$1</b></size>", RegexOptions.Multiline);

			// H3 (### Titre)
			text = ConvertPattern(text, @"^###\s+(.+)$", "<size=150%><b>$1</b></size>", RegexOptions.Multiline);

			// H4 (#### Titre)
			text = ConvertPattern(text, @"^####\s+(.+)$", "<size=125%><b>$1</b></size>", RegexOptions.Multiline);

			return text;
		}

		private static StringBuilder ConvertPattern(StringBuilder text, string pattern, string replacement, RegexOptions options = RegexOptions.None) {
			var result = Regex.Replace(text.ToString(), pattern, replacement, options);
			text.Clear();
			text.Append(result);
			return text;
		}
	}
}