using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Players;
using Nox.Users;
using Nox.Worlds.Spawns;
using Logger = Nox.CCK.Utils.Logger;
using NoxTransform = Nox.CCK.Utils.Transform;
using Transform = UnityEngine.Transform;

namespace api.nox.offline {
	public class OfflinePlayer : IPlayer {
		internal OfflinePlayer(OfflineAdapter context, int id) {
			_id          = id;
			_context     = context;
			CreationTime = DateTime.UtcNow;
			_properties  = new Dictionary<string, object>();
		}

		private readonly  int                        _id;
		private readonly  Dictionary<string, object> _properties;
		private readonly  OfflineAdapter             _context;
		internal readonly DateTime                   CreationTime;

		[NoxPublic(NoxAccess.Method)]
		public int GetId()
			=> _id;

		[NoxPublic(NoxAccess.Method)]
		public bool IsLocal()
			=> true;

		public IUserIdentifier ToIdentifier()
			=> Main.UserAPI.GetCurrent()?.ToIdentifier();

		[NoxPublic(NoxAccess.Method)]
		public bool IsMaster()
			=> _context.GetMasterPlayer()?.GetId() == _id;

		[NoxPublic(NoxAccess.Method)]
		public string GetDisplay()
			=> Main.UserAPI.GetCurrent()?.GetDisplay();

		[NoxPublic(NoxAccess.Method)]
		public void SetDisplay(string display)
			=> Logger.LogWarning("OfflinePlayer does not support changing display name.");

		[NoxPublic(NoxAccess.Method)]
		public Dictionary<string, object> GetProperties()
			=> _properties;

		[NoxPublic(NoxAccess.Method)]
		public T GetProperty<T>(string key, T defaultValue) where T : struct {
			if (_properties.TryGetValue(key, out var value) && value is T typedValue)
				return typedValue;
			return defaultValue;
		}

		[NoxPublic(NoxAccess.Method)]
		public void SetProperty<T>(string key, T value) where T : struct
			=> _properties[key] = value;

		[NoxPublic(NoxAccess.Method)]
		public void RemoveProperty(string key) {
			if (_properties.ContainsKey(key))
				_properties.Remove(key);
		}

		private static bool TryGetPart(ushort index, out Transform part) {
			var controller = Main.ControllerAPI.GetCurrent();
			if (controller != null)
				return controller.TryGetPart(index, out part);
			part = null;
			return false;
		}

		private static void SetPart(ushort index, NoxTransform part) {
			var controller = Main.ControllerAPI.GetCurrent();
			if (controller != null)
				controller.SetPart(index, part);
		}

		[NoxPublic(NoxAccess.Method)]
		public Vector3 GetPosition()
			=> TryGetPart(PlayerRig.Base.ToIndex(), out var transform)
				? transform.position
				: Vector3.zero;

		[NoxPublic(NoxAccess.Method)]
		public Quaternion GetRotation()
			=> TryGetPart(PlayerRig.Base.ToIndex(), out var transform)
				? transform.rotation
				: Quaternion.identity;

		[NoxPublic(NoxAccess.Method)]
		public void SetPosition(Vector3 position) {
			var nox = new NoxTransform();
			nox.SetPosition(position);
			SetPart(PlayerRig.Base.ToIndex(), nox);
		}

		[NoxPublic(NoxAccess.Method)]
		public void SetRotation(Quaternion rotation) {
			var nox = new NoxTransform();
			nox.SetRotation(rotation);
			SetPart(PlayerRig.Base.ToIndex(), nox);
		}

		[NoxPublic(NoxAccess.Method)]
		public Vector3 GetVelocity()
			=> TryGetPart(PlayerRig.Base.ToIndex(), out var transform)
				? transform.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero
				: Vector3.zero;

		// ReSharper disable Unity.PerformanceAnalysis
		[NoxPublic(NoxAccess.Method)]
		public void SetVelocity(Vector3 velocity) {
			var nox = new NoxTransform();
			nox.SetVelocity(velocity);
			SetPart(PlayerRig.Base.ToIndex(), nox);
		}

		[NoxPublic(NoxAccess.Method)]
		public Vector3 GetAngularVelocity()
			=> TryGetPart(PlayerRig.Base.ToIndex(), out var transform)
				? transform.GetComponent<Rigidbody>()?.angularVelocity ?? Vector3.zero
				: Vector3.zero;

		// ReSharper disable Unity.PerformanceAnalysis
		[NoxPublic(NoxAccess.Method)]
		public void SetAngularVelocity(Vector3 angular) {
			var nox = new NoxTransform();
			nox.SetAngularVelocity(angular);
			SetPart(PlayerRig.Base.ToIndex(), nox);
		}

		public bool TryGetPhysical<T>(out T physical) where T : Physical {
			Logger.LogWarning("OfflinePlayer does not support physical objects.");
			physical = null;
			return false;
		}

		public bool MakePhysical() {
			Logger.LogWarning("OfflinePlayer does not support physical objects.");
			return false;
		}

		public void DestroyPhysical() {
			Logger.LogWarning("OfflinePlayer does not support physical objects.");
		}

		[NoxPublic(NoxAccess.Method)]
		public void Teleport(Vector3 position, Quaternion rotation)
			=> Teleport(position, rotation, Vector3.zero, Vector3.zero);

		public void Teleport(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angular) {
			var nox = new NoxTransform();
			nox.SetPosition(position);
			nox.SetRotation(rotation);
			nox.SetVelocity(velocity);
			nox.SetAngularVelocity(angular);
			SetPart(PlayerRig.Base.ToIndex(), nox);
		}


		[NoxPublic(NoxAccess.Method)]
		public void Teleport(Transform transform, Rigidbody rigidbody = null) {
			var nox = new NoxTransform();

			nox.SetPosition(transform.position);
			nox.SetRotation(transform.rotation);

			if (rigidbody) {
				nox.SetVelocity(rigidbody.linearVelocity);
				nox.SetAngularVelocity(rigidbody.angularVelocity);
			}

			SetPart(PlayerRig.Base.ToIndex(), nox);
		}

		// ReSharper disable Unity.PerformanceAnalysis
		public void MovePart(ushort part, NoxTransform transform) {
			if (!TryGetPart(part, out var tr)) return;

			if (!transform.IsSamePosition(tr.position))
				tr.position = transform.GetPosition();

			if (!transform.IsSameRotation(tr.rotation))
				tr.rotation = transform.GetRotation();

			if (!transform.IsSameScale(tr.localScale))
				tr.localScale = transform.GetScale();

			if (!tr.TryGetComponent(out Rigidbody rb))
				return;

			if (!transform.IsSameVelocity(rb.linearVelocity))
				rb.angularVelocity = transform.GetAngularVelocity();

			if (!transform.IsSameAngularVelocity(rb.angularVelocity))
				rb.angularVelocity = transform.GetAngularVelocity();
		}

		public void Respawn() {
			var dimension   = _context.GetDimension();
			var main        = dimension.GetScene().GetInstances()[0];
			var descriptor  = main.GetDescriptor(dimension.GetMainIndex());
			var spawnModule = descriptor?.GetModules<ISpawnModule>().FirstOrDefault();
			if (spawnModule == null) return;
			var spawn = spawnModule.ChoiceSpawn();
			Teleport(spawn.GetPosition(), spawn.GetRotation());
			Logger.LogDebug($"Player {_id} respawned at {spawn.GetPosition()}");
		}

		public void Move(NoxTransform transform)
			=> MovePart(PlayerRig.Base.ToIndex(), transform);
	}
}