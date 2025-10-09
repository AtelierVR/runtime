using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Nox.ModLoader;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Editor {
	public class ModLinker : UnityEditor.AssetModificationProcessor {
		private static string[] OnWillSaveAssets(string[] paths) {
			if (paths.Any(path => path.EndsWith(".asmdef")))
				ModLinkerHelper.EnsureLinkerClassExists();
			return paths;
		}
	}

	public static class ModLinkerHelper {
		private const string LinkXmlName = "link.xml";

		public static void EnsureLinkerClassExists() {
			var li = new List<string>();

			foreach (var (path, fullNames) in GetAssemblyByMod()) {
				UpdateLinkXml(path, fullNames);
				li.AddRange(fullNames);
			}

			UpdateLinkXml(
				Path.Combine(Application.dataPath, LinkXmlName),
				li.Distinct().ToArray()
			);
		}

		private static (string, string[])[] GetAssemblyByMod() {
			var assetMods   = Directory.GetFiles("Assets", "nox.mod.*", SearchOption.AllDirectories);
			var packageMods = Directory.GetFiles("Packages", "nox.mod.*", SearchOption.AllDirectories);
			return (from mod in assetMods.Concat(packageMods).Distinct().ToArray()
				select Path.GetDirectoryName(mod) into dir
				let def = Directory.GetFiles(dir, "*.asmdef", SearchOption.AllDirectories)
				let asmNames = def.Select(Path.GetFileNameWithoutExtension).ToArray()
				select (Path.Combine(dir, LinkXmlName), asmNames)).ToArray();
		}


		private static void UpdateLinkXml(string path, string[] assemblies) {
			try {
				XmlDocument doc;
				XmlElement  linkerElement;

				if (File.Exists(path)) {
					// Charger le fichier existant
					doc = new XmlDocument();
					doc.Load(path);
					linkerElement = doc.DocumentElement;
				} else {
					// Créer un nouveau fichier
					doc = new XmlDocument();
					doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
					linkerElement = doc.CreateElement("linker");
					doc.AppendChild(linkerElement);
				}

				// Supprimer les anciennes entrées de mods
				var existingAssemblies = linkerElement?.SelectNodes("assembly");
				var toRemove           = new List<XmlNode>();

				if (existingAssemblies != null) {
					toRemove.AddRange(
						from XmlNode assemblyNode in existingAssemblies
						let nameAttr = assemblyNode.Attributes?["fullname"]
						select assemblyNode
					);

					foreach (var node in toRemove)
						linkerElement.RemoveChild(node);
				}

				// Ajouter les nouvelles entrées
				foreach (var assembly in assemblies.OrderBy(a => a)) {
					var assemblyElement = doc.CreateElement("assembly");
					assemblyElement.SetAttribute("fullname", assembly);
					assemblyElement.SetAttribute("preserve", "all");
					if (linkerElement != null) linkerElement.AppendChild(assemblyElement);
				}

				// Sauvegarder le fichier
				var settings = new XmlWriterSettings {
					Indent       = true,
					IndentChars  = "\t",
					NewLineChars = "\n"
				};

				using var writer = XmlWriter.Create(path, settings);
				doc.Save(writer);
			} catch (Exception e) {
				Logger.LogError($"Failed to update link.xml at {path}: {e}");
				Logger.LogException(e);
			}
		}
	}
}