using System;
using System.Linq;
using Nox.CCK.Build;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Worlds {
	public abstract class BaseWorldDescriptor : MonoBehaviour, IBaseWorldDescriptor, ICompilable {
		public GameObject[] spawns;
		public SpawnType    spawnType  = SpawnType.Random;
		public int          spawnIndex = 0;

		public SpawnType GetSpawnType()
			=> spawnType;

		public int GetSpawnIndex()
			=> spawnIndex;

		public int NextSpawnIndex() {
			var sp = GetSpawns();
			if (sp == null || sp.Length == 0)
				return -1;
			spawnIndex = (spawnIndex + 1) % sp.Length;
			return spawnIndex;
		}

		public GameObject GetRoot()
			=> gameObject;

		#if UNITY_EDITOR
		public bool isCompiled;

		public GameObject[] GetSpawns() {
			var sp = spawns ?? Array.Empty<GameObject>();
			return sp.Length > 0 ? sp : new[] { gameObject };
		}

		public virtual void Compile() {
			spawns = this.EstimateSpawns().Values.ToArray();
			if (spawnIndex < 0 || spawnIndex >= spawns.Length)
				spawnIndex = 0;
			isCompiled = true;
			Logger.LogDebug($"{GetType().Name} compiled.", this);
		}

		#else
		public GameObject[] GetSpawns()
			=> spawns ?? Array.Empty<GameObject>();
		#endif
	}
}