using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Nox.ModLoader;
using UnityEditor;
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
		

		// Assemblies that must always be preserved regardless of mod discovery
		// (system-level packages with no nox.mod.json, or precompiled DLLs)
		private static readonly string[] AlwaysPreservedAssemblies = {
			"Nox.ModLoader",
			"Mono.Cecil",
			"Mono.Cecil.Mdb",
			"Mono.Cecil.Pdb",
			"Mono.Cecil.Rocks",
		};

		[InitializeOnLoadMethod, MenuItem("Nox/Tools/Update Linker Files")]
		public static void EnsureLinkerClassExists() {
			var li = new List<string>();

			foreach (var (path, fullNames) in GetAssemblyByMod()) {
				UpdateLinkXml(path, fullNames);
				li.AddRange(fullNames);
			}

			UpdateLinkXml(
				Path.Combine(Application.dataPath, LinkXmlName),
				li.Concat(AlwaysPreservedAssemblies).Distinct().ToArray()
			);
		}

		private static (string, string[])[] GetAssemblyByMod() {
			var assetMods   = Directory.GetFiles("Assets", "nox.mod.*", SearchOption.AllDirectories);
			var packageMods = Directory.GetFiles("Packages", "nox.mod.*", SearchOption.AllDirectories);
			return (from mod in assetMods.Concat(packageMods).Distinct().ToArray()
				select Path.GetDirectoryName(mod) into dir
				let def = Directory.GetFiles(dir, "*.asmdef", SearchOption.AllDirectories)
				let asmNames = def.Select(Path.GetFileNameWithoutExtension)
				let pluginNames = GetManagedPluginAssemblyNames(dir)
				select (Path.Combine(dir, LinkXmlName), asmNames.Concat(pluginNames).Distinct().ToArray())).ToArray();
		}

		private static IEnumerable<string> GetManagedPluginAssemblyNames(string modDir) {
			var pluginsDir = Path.Combine(modDir, "Plugins");
			if (!Directory.Exists(pluginsDir))
				yield break;

			foreach (var dll in Directory.GetFiles(pluginsDir, "*.dll", SearchOption.AllDirectories)) {
				string name = null;
				try {
					name = AssemblyName.GetAssemblyName(dll).Name;
				} catch {
					// native DLL or invalid managed assembly — skip
				}
				if (name != null)
					yield return name;
			}
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
				Logger.LogError(e);
			}
		}
	}
}