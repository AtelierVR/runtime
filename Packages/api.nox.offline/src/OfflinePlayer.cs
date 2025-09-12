using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars;
using UnityEngine;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Players;
using Nox.Users;
using Nox.Worlds.Spawns;
using Logger = Nox.CCK.Utils.Logger;
using Transform = Nox.CCK.Utils.Transform;

namespace api.nox.offline {
	public class OfflinePlayer : IPlayer {
		internal OfflinePlayer(OfflineAdapter context, int id) {
			_id          = id;
			_context     = context;
			CreationTime = DateTime.UtcNow;
			_properties  = new Dictionary<string, object>();
		}

		private           string                        _name = "Offline Player";
		private readonly  int                           _id;
		private readonly  Dictionary<string, object>    _properties;
		internal readonly Dictionary<ushort, Transform> Transforms = new();
		private readonly  OfflineAdapter                _context;
		internal readonly DateTime                      CreationTime;
		internal readonly IUserIdentifier               Identifier = null;

		[NoxPublic(NoxAccess.Method)]
		public int GetId()
			=> _id;

		[NoxPublic(NoxAccess.Method)]
		public bool IsLocal()
			=> true;

		public IUserIdentifier ToIdentifier()
			=> Identifier;

		[NoxPublic(NoxAccess.Method)]
		public bool IsMaster()
			=> _context.GetMasterPlayer()?.GetId() == _id;

		[NoxPublic(NoxAccess.Method)]
		public string GetDisplay()
			=> _name;

		[NoxPublic(NoxAccess.Method)]
		public void SetDisplay(string display)
			=> _name = display;

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

		[NoxPublic(NoxAccess.Method)]
		public Vector3 GetPosition()
			=> Transforms.TryGetValue(PlayerRig.Base.ToIndex(), out var transform)
				? transform.GetPosition()
				: Vector3.zero;

		[NoxPublic(NoxAccess.Method)]
		public Quaternion GetRotation()
			=> Transforms.TryGetValue(PlayerRig.Base.ToIndex(), out var transform)
				? transform.GetRotation()
				: Quaternion.identity;

		[NoxPublic(NoxAccess.Method)]
		public void SetPosition(Vector3 position) {
			if (!Transforms.TryGetValue(PlayerRig.Base.ToIndex(), out var tr)) return;
			tr.DeliveryType = DeliveryType.LocalModified;
			tr.SetPosition(position);
			Transforms[PlayerRig.Base.ToIndex()] = tr;
		}

		[NoxPublic(NoxAccess.Method)]
		public void SetRotation(Quaternion rotation) {
			if (!Transforms.TryGetValue(PlayerRig.Base.ToIndex(), out var tr)) return;
			tr.DeliveryType = DeliveryType.LocalModified;
			tr.SetRotation(rotation);
			Transforms[PlayerRig.Base.ToIndex()] = tr;
		}

		[NoxPublic(NoxAccess.Method)]
		public Vector3 GetVelocity()
			=> Transforms.TryGetValue(PlayerRig.Base.ToIndex(), out var transform)
				? transform.GetVelocity()
				: Vector3.zero;

		[NoxPublic(NoxAccess.Method)]
		public void SetVelocity(Vector3 velocity) {
			if (!Transforms.TryGetValue(PlayerRig.Base.ToIndex(), out var tr)) return;
			tr.DeliveryType = DeliveryType.LocalModified;
			tr.SetVelocity(velocity);
			Transforms[PlayerRig.Base.ToIndex()] = tr;
		}

		[NoxPublic(NoxAccess.Method)]
		public Vector3 GetAngularVelocity()
			=> Transforms.TryGetValue(PlayerRig.Base.ToIndex(), out var transform)
				? transform.GetAngularVelocity()
				: Vector3.zero;

		[NoxPublic(NoxAccess.Method)]
		public void SetAngularVelocity(Vector3 angular) {
			if (!Transforms.TryGetValue(PlayerRig.Base.ToIndex(), out var tr)) return;
			tr.DeliveryType = DeliveryType.LocalModified;
			tr.SetAngularVelocity(angular);
			Transforms[PlayerRig.Base.ToIndex()] = tr;
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
		public void Teleport(Vector3 position, Quaternion rotation) {
			if (!Transforms.TryGetValue(PlayerRig.Base.ToIndex(), out var tr)) return;
			tr.DeliveryType = DeliveryType.LocalModified;
			tr.SetPosition(position);
			tr.SetRotation(rotation);
			Transforms[PlayerRig.Base.ToIndex()] = tr;
		}


		[NoxPublic(NoxAccess.Method)]
		public void Teleport(UnityEngine.Transform transform) {
			if (!transform) return;
			Teleport(transform.position, transform.rotation);
		}

		public void MovePart(ushort part, Transform transform) {
			if (!Transforms.TryGetValue(part, out var tr)) return;
			transform.DeliveryType = DeliveryType.LocalModified;
			Transforms[part]       = transform;
		}

		public void Respawn() {
			var dimension   = _context.GetDimension();
			var main        = dimension.GetScene().GetInstances()[0];
			var descriptor  = main.GetInstanceDescriptor(dimension.GetMainIndex());
			var spawnModule = descriptor?.GetModules<ISpawnModule>().FirstOrDefault();
			if (spawnModule == null) return;
			var spawn = spawnModule.ChoiceSpawn();
			Teleport(spawn.GetPosition(), spawn.GetRotation());
		}

		public void Move(Transform transform)
			=> MovePart(PlayerRig.Base.ToIndex(), transform);

		public void SetAvatar(IAvatarIdentifier avatar) {
			Logger.LogWarning("OfflinePlayer does not support avatars.");
		}
	}
}