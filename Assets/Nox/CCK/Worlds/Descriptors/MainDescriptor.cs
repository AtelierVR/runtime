#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace Nox.CCK.Worlds
{
    [SerializeField]
    public class MainDescriptor : BaseDescriptor
    {

        [SerializeField] private string[] data_Scenes;
        [SerializeField] private string[] data_Features;
        [SerializeField] private string[] data_Mods;


#if UNITY_EDITOR
        [SerializeField] public List<SceneAsset> Scenes = new();
        [SerializeField] public List<string> Features = new();
        [SerializeField] public List<ModRequirement> ModRequirements = new();
        [SerializeField] private SupportBuildTarget _target = SupportBuildTarget.NoTarget;
        [SerializeField] public string ServerPublisher = null;
        [SerializeField] public uint IdPublisher = 0;
        [SerializeField] public ushort VersionPublisher = 0;

        public SupportBuildTarget target
        {
            get => _target == SupportBuildTarget.NoTarget ? SuppordTarget.GetCurrentTarget() : _target;
            set
            {
                _target = value;
                EditorUtility.SetDirty(this);
            }
        }


        public override void Compile()
        {
            base.Compile();
            var sceneSet = EstimateScenes();
            Scenes = sceneSet.Values.ToList();
            var lscenes = new List<string>();
            foreach (var scene in Scenes)
                lscenes.Add(AssetDatabase.GetAssetPath(scene));
            data_Scenes = lscenes.ToArray();
            data_Type = DescriptorType.Main;
            data_Features = EstimateFeatures().Values.ToArray();
            data_Mods = EstimateMods().Values.Select(mod => mod.Id).ToArray();
            EditorUtility.SetDirty(this);
        }

        public Dictionary<byte, SceneAsset> EstimateScenes()
        {
            var sceneSet = new HashSet<SceneAsset> { AssetDatabase.LoadAssetAtPath<SceneAsset>(gameObject.scene.path) };
            foreach (var scenePath in Scenes)
                if (scenePath != null && !sceneSet.Contains(scenePath))
                    sceneSet.Add(scenePath);
            var scenesDict = new Dictionary<byte, SceneAsset>();
            for (byte i = 0; i < sceneSet.Count; i++)
            {
                byte estimatedId = i;
                while (scenesDict.ContainsKey(estimatedId)) estimatedId++;
                scenesDict.Add(estimatedId, sceneSet.ToArray()[i]);
            }
            return scenesDict;
        }

        public Dictionary<ushort, string> EstimateFeatures()
        {
            var featureSet = new HashSet<string>();
            foreach (var feature in Features)
                if (!string.IsNullOrWhiteSpace(feature) && !featureSet.Contains(feature))
                    featureSet.Add(feature.Trim());
            var featuresDict = new Dictionary<ushort, string>();
            for (ushort i = 0; i < featureSet.Count; i++)
            {
                ushort estimatedId = i;
                while (featuresDict.ContainsKey(estimatedId)) estimatedId++;
                featuresDict.Add(estimatedId, featureSet.ToArray()[i]);
            }
            return featuresDict;
        }

        public Dictionary<ushort, ModRequirement> EstimateMods()
        {
            var modSet = new HashSet<ModRequirement>();
            foreach (var mod in ModRequirements)
                if (mod != null && !modSet.Contains(mod))
                    modSet.Add(mod);
            var modsDict = new Dictionary<ushort, ModRequirement>();
            for (ushort i = 0; i < modSet.Count; i++)
            {
                ushort estimatedId = i;
                while (modsDict.ContainsKey(estimatedId)) estimatedId++;
                modsDict.Add(estimatedId, modSet.ToArray()[i]);
            }
            return modsDict;
        }

        public List<string> GetScenes()
        {
            if (IsCompiled) return (data_Scenes ?? new string[0]).ToList();
            return Scenes.Select(scene => AssetDatabase.GetAssetPath(scene)).ToList();
        }

        public List<string> GetFeatures()
        {
            if (IsCompiled) return (data_Features ?? new string[0]).ToList();
            return Features;
        }

        public List<ModRequirement> GetRequirementMods()
        {
            if (IsCompiled) return (data_Mods?.Select(s => new ModRequirement { Id = s }) ?? new ModRequirement[0]).ToList();
            return ModRequirements;
        }

        public List<string> GetMods()
        {
            if (IsCompiled) return (data_Mods ?? new string[0]).ToList();
            return ModRequirements.Select(mod => mod?.Id).ToList();
        }

        public SupportBuildTarget GetBuildPlatform() => target;
#else
        public List<string> GetScenes() => ( data_Scenes ?? new string[0]).ToList();
        public List<string> GetFeatures() => ( data_Features ?? new string[0]).ToList();
        public List<string> GetMods() => ( data_Mods ?? new string[0]).ToList();
#endif
    }


    [Serializable]
    public class ModRequirement
    {
        public string Id;
        public ModRequirmentFlags Flags;
    }

    [Flags]
    public enum ModRequirmentFlags
    {
        None = 0,
        IsRequiredForMaster = 2,
        IsRequiredForBot = 4,
        IsRequiredForPlayer = 8,
        IsRequiredForAll = 14
    }

}