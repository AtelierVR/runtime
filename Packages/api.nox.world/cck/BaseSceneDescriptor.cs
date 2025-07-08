using System;
using System.Linq;
using Nox.CCK.Build;
using UnityEngine;
using UnityEngine.Serialization;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Worlds {
	public abstract class BaseSceneDescriptor : MonoBehaviour, ICompilable {
		public GameObject[] spawns;
		public SpawnType    spawnType  = SpawnType.Random;
		public int          spawnIndex = 0;

		public SpawnType GetSpawnType()
			=> spawnType;

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