using System;
using System.Collections.Generic;
using UnityEngine;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Players;
using Nox.Users;
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
			tr.SetPosition(position);
			Transforms[PlayerRig.Base.ToIndex()] = tr;
		}

		[NoxPublic(NoxAccess.Method)]
		public void SetRotation(Quaternion rotation) {
			if (!Transforms.TryGetValue(PlayerRig.Base.ToIndex(), out var tr)) return;
			tr.SetRotation(rotation);
			Transforms[PlayerRig.Base.ToIndex()] = tr;
		}
		
		public bool TryGetPhysical(out Physical physical) {
			physical = null;
			return false;
		}

		[NoxPublic(NoxAccess.Method)]
		public void Teleport(Vector3 position, Quaternion rotation) {
			SetPosition(position);
			SetRotation(rotation);
		}


		[NoxPublic(NoxAccess.Method)]
		public void Teleport(UnityEngine.Transform transform) {
			if (!transform) return;
			Teleport(transform.position, transform.rotation);
		}
	}
}