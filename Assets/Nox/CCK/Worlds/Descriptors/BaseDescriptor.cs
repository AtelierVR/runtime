// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using System.Linq;
// using UnityEngine.SceneManagement;
// using Gizmos = Nox.CCK.Development.Gizmos;
// using Nox.CCK.Development;
// using Nox.CCK.Utils;
// using Logger = Nox.CCK.Utils.Logger;
// using Random = UnityEngine.Random;
//
// namespace Nox.CCK.Worlds {
// 	[Gizmos("cck.worlds.descriptors")]
// 	public abstract class BaseDescriptor : MonoBehaviour {
// 		[SerializeField] private   SpawnType                  data_SpawnType;
// 		[SerializeField] private   NetworkObject[]            data_NetworkObjects;
// 		[SerializeField] private   GameObject[]               data_Spawns;
// 		[SerializeField] private   bool                       data_FlyOnSpawn;
// 		[SerializeField] private   double                     data_RespawnHeight;
// 		[SerializeField] protected DescriptorType             data_Type = DescriptorType.None;
// 		[SerializeField] private   AudioChannelDescriptor[]   data_audios;
// 		[SerializeField] private   Dictionary<string, byte[]> data_custom = new();
// 		// [SerializeField] private JintScript[] data_jintScripts;
//
// 		public static T[] GetDescriptors<T>() where T : BaseDescriptor {
// 			var descriptors = new List<T>();
// 			for (var i = 0; i < SceneManager.sceneCount; i++)
// 				if (TryGetDescriptor<T>(SceneManager.GetSceneAt(i), out var descriptor))
// 					descriptors.Add(descriptor);
// 			return descriptors.ToArray();
// 		}
//
// 		public static bool TryGetDescriptor<T>(GameObject root, out T descriptor) where T : BaseDescriptor {
// 			if (root.TryGetComponent(out descriptor))
// 				return true;
//
// 			foreach (UnityEngine.Transform child in root.transform)
// 				if (TryGetDescriptor(child.gameObject, out descriptor))
// 					return true;
//
// 			descriptor = null;
// 			return false;
// 		}
//
// 		public static bool TryGetDescriptor<T>(Scene scene, out T descriptor) where T : BaseDescriptor {
// 			if (!scene.IsValid() || !scene.isLoaded) {
// 				descriptor = null;
// 				return false;
// 			}
//
// 			foreach (var root in scene.GetRootGameObjects())
// 				if (TryGetDescriptor(root, out descriptor))
// 					return true;
//
// 			descriptor = null;
// 			return false;
// 		}
//
// 		public static AudioChannel[] GetAudioChannels() {
// 			List<AudioChannel> channels = new();
// 			for (var i = 0; i < SceneManager.sceneCount; i++) {
// 				var scene = SceneManager.GetSceneAt(i);
// 				var fcs   = GetAudioChannels(scene);
// 				channels.AddRange(fcs);
// 			}
//
// 			return channels.ToArray();
// 		}
//
// 		public static AudioChannel[] GetAudioChannels(Scene scene)
// 			=> scene.GetRootGameObjects()
// 				.Select(Finder.FindComponent<AudioChannel>)
// 				.Where(channel => channel != null)
// 				.ToArray();
//
// 		public uint SequentialIndex = 0;
//
// 		public GameObject ChoiceSpawn(List<GameObject> spawns = null, SpawnType type = SpawnType.None) {
// 			spawns ??= GetSpawns();
// 			if (type         == SpawnType.None) type = GetSpawnType();
// 			if (spawns.Count == 0) return gameObject;
// 			var o = type switch {
// 				SpawnType.First      => spawns[0],
// 				SpawnType.Random     => spawns[Random.Range(0, spawns.Count)],
// 				SpawnType.Free       => spawns.FirstOrDefault(spawn => !spawn.activeSelf),
// 				SpawnType.Sequential => spawns[(int)(SequentialIndex++ % spawns.Count)],
// 				_                    => null,
// 			};
// 			return !o ? gameObject : o;
// 		}
//
// 		public bool HasCustom(string key)
// 			=> data_custom.ContainsKey(key);
//
// 		public void RemoveCustom(string key)
// 			=> data_custom.Remove(key);
//
// 		public byte[] GetCustom(string key)
// 			=> HasCustom(key) ? data_custom[key] : null;
//
// 		public void SetCustom(string key, byte[] value) {
// 			if (HasCustom(key)) data_custom[key] = value;
// 			else data_custom.Add(key, value);
// 		}
//
// 		public bool TryGetCustom(string key, out byte[] value) {
// 			if (HasCustom(key)) {
// 				value = data_custom[key];
// 				return true;
// 			}
//
// 			value = null;
// 			return false;
// 		}
//
// 		#if UNITY_EDITOR
//
// 		public bool IsCompiled
// 			=> data_Type != DescriptorType.None;
//
// 		public virtual void Compile() {
// 			var netSet = EstimateNetworkObjects();
// 			foreach (var entry in netSet)
// 				entry.Value.networkId = entry.Key;
//
// 			NetworkObjects      = netSet.Values.ToList();
// 			data_Type           = DescriptorType.Main;
// 			data_NetworkObjects = NetworkObjects.ToArray();
// 			data_Spawns         = EstimateSpawns().Values.ToArray();
// 			// data_jintScripts = EstimateJintScripts().Values.ToArray();
// 			data_SpawnType     = SpawnType;
// 			data_RespawnHeight = RespawnHeight;
// 			data_FlyOnSpawn    = FlyOnSpawn;
// 			data_audios        = EstimateAudioChannels().Values.ToArray();
// 		}
//
// 		public List<NetworkObject>          NetworkObjects = new();
// 		public List<GameObject>             Spawns         = new();
// 		public bool                         FlyOnSpawn;
// 		public List<AudioChannelDescriptor> AudioChannels = new();
// 		public SpawnType                    SpawnType     = SpawnType.First;
// 		public double                       RespawnHeight = -100;
// 		// public List<JintScript> JintScripts = new();
//
// 		private static T Choice<T>(T a, T b)
// 			=> a == null ? b : a;
//
// 		public Dictionary<ushort, NetworkObject> EstimateNetworkObjects() {
// 			var netSet = new HashSet<NetworkObject>();
// 			foreach (var obj in NetworkObjects.Where(obj => obj)) netSet.Add(obj);
// 			var networkObjectsDict = new Dictionary<ushort, NetworkObject>();
// 			for (ushort i = 0; i < netSet.Count; i++) {
// 				var estimatedId = Choice(netSet.ToArray()[i].networkId, i);
// 				while (networkObjectsDict.ContainsKey(Choice(estimatedId, i))) estimatedId++;
// 				networkObjectsDict.Add(Choice(estimatedId, (ushort)(i + 1)), netSet.ToArray()[i]);
// 			}
//
// 			return networkObjectsDict;
// 		}
//
// 		public Dictionary<byte, GameObject> EstimateSpawns() {
// 			var spawnSet = new HashSet<GameObject> { gameObject };
// 			foreach (var spawnObj in Spawns.Where(spawnObj => spawnObj)) spawnSet.Add(spawnObj);
// 			var spawnsDict = new Dictionary<byte, GameObject>();
// 			for (byte i = 0; i < spawnSet.Count; i++) {
// 				var estimatedId = i;
// 				while (spawnsDict.ContainsKey(estimatedId)) estimatedId++;
// 				spawnsDict.Add(estimatedId, spawnSet.ToArray()[i]);
// 			}
//
// 			return spawnsDict;
// 		}
//
// 		public Dictionary<string, AudioChannelDescriptor> EstimateAudioChannels() {
// 			var audioSet = new HashSet<AudioChannelDescriptor>();
// 			foreach (var item in AudioChannels.Where(item => item != null)) audioSet.Add(item);
// 			var audioDict = new Dictionary<string, AudioChannelDescriptor>();
// 			for (byte i = 0; i < audioSet.Count; i++) {
// 				var estimatedId                                        = Choice(audioSet.ToArray()[i].id, i.ToString());
// 				while (audioDict.ContainsKey(estimatedId)) estimatedId += "_";
// 				audioDict.Add(estimatedId, audioSet.ToArray()[i]);
// 			}
//
// 			return audioDict;
// 		}
//
// 		// public Dictionary<string, JintScript> EstimateJintScripts()
// 		// {
// 		//     var scriptSet = new HashSet<JintScript>();
// 		//     foreach (var scriptObj in JintScripts.Where(scriptObj => scriptObj)) scriptSet.Add(scriptObj);
// 		//     var scriptDict = new Dictionary<string, JintScript>();
// 		//     for (byte i = 0; i < scriptSet.Count; i++)
// 		//     {
// 		//         var estimatedId = Choice(scriptSet.ToArray()[i].name, i.ToString());
// 		//         while (scriptDict.ContainsKey(estimatedId)) estimatedId += "_";
// 		//         scriptDict.Add(estimatedId, scriptSet.ToArray()[i]);
// 		//     }
// 		//     return scriptDict;
// 		// }
//
//
// 		public List<NetworkObject> GetNetworkObjects()
// 			=> IsCompiled ? (data_NetworkObjects ?? Array.Empty<NetworkObject>()).ToList() : NetworkObjects;
//
// 		public List<GameObject> GetSpawns()
// 			=> IsCompiled ? (data_Spawns ?? Array.Empty<GameObject>()).ToList() : Spawns;
//
// 		public SpawnType GetSpawnType()
// 			=> IsCompiled ? data_SpawnType : SpawnType;
//
// 		public double GetRespawnHeight()
// 			=> IsCompiled ? data_RespawnHeight : RespawnHeight;
//
// 		public List<AudioChannelDescriptor> GetAudioChannelDescriptors()
// 			=> IsCompiled ? (data_audios ?? Array.Empty<AudioChannelDescriptor>()).ToList() : AudioChannels;
//
// 		// public List<JintScript> GetJintScripts()
// 		//     => IsCompiled ? (data_jintScripts ?? Array.Empty<JintScript>()).ToList() : JintScripts;
//
// 		public bool GetFlyOnSpawn()
// 			=> IsCompiled ? data_FlyOnSpawn : FlyOnSpawn;
//
// 		public virtual void OnDrawGizmos() {
// 			var isSpawnSelected = Spawns.Any(spawn => UnityEditor.Selection.activeGameObject == spawn);
// 			if (!isSpawnSelected) return;
//
// 			Gizmos.color = Color.gray;
// 			Gizmos.DrawWireSphere(transform.position, 0.1f);
//
// 			Gizmos.color = new Color(
// 				Color.yellow.r,
// 				Color.yellow.g,
// 				Color.yellow.b,
// 				0.1f
// 			);
// 			var       cameraPosition = UnityEditor.SceneView.currentDrawingSceneView.camera.transform.position;
// 			const int size           = 10;
// 			Gizmos.DrawCube(
// 				new Vector3(
// 					cameraPosition.x,
// 					(float)RespawnHeight,
// 					cameraPosition.z
// 				), new Vector3(size, 0, size)
// 			);
//
// 			var spawns = GetSpawns();
// 			foreach (var spawn in spawns.Where(spawn => spawn != null)) {
// 				Gizmos.color = Color.white;
// 				Gizmos.DrawLine(transform.position, spawn.transform.position);
//
// 				Gizmos.color = Color.blue;
// 				Gizmos.DrawWireSphere(spawn.transform.position, 0.5f);
//
// 				Gizmos.color = Color.yellow;
// 				Gizmos.DrawLine(spawn.transform.position, spawn.transform.position + spawn.transform.forward);
// 			}
// 		}
//
// 		#else
//         public List<NetworkObject> GetNetworkObjects() => ( data_NetworkObjects ?? new NetworkObject[0]).ToList();
//         public List<GameObject> GetSpawns() => ( data_Spawns ?? new GameObject[0]).ToList();
//         public SpawnType GetSpawnType() => data_SpawnType == SpawnType.None ? SpawnType.First : data_SpawnType;
//         public double GetRespawnHeight() => data_RespawnHeight;
//         public List<AudioChannelDescriptor> GetAudioChannelDescriptors() => (data_audios ?? new AudioChannelDescriptor[0]).ToList();
//         public bool GetFlyOnSpawn() => data_FlyOnSpawn;
// 		#endif
// 	}
//
//
// 	public enum DescriptorType {
// 		None = 0,
// 		Main = 1,
// 		Sub  = 2
// 	}
//
// 	public class AudioChannelDescriptor {
// 		public string id;
// 		public string title_key;
// 		public bool   is_world_channel;
// 	}
// }