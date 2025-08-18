using System.Collections.Generic;
using Nox.Avatars;
using Nox.Entities;
using Nox.Players;
using Nox.Users;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.avatar.editor {
	public class TestAvatar : Physical, IPlayer {
		public int GetId()
			=> GetInstanceID();

		private Avatar _avatar;
		public  bool   isLocal     = true;
		public  bool   isMaster    = true;
		public  string displayName = "TestAvatar";

		public          IAvatarDescriptor          Descriptor;
		public readonly Dictionary<string, object> Properties = new();

		public Transform GetTransform()
			=> transform;

		public Rigidbody GetRigidbody()
			=> GetComponent<Rigidbody>();

		public Dictionary<string, object> GetProperties()
			=> Properties;

		public T GetProperty<T>(string key, T defaultValue) where T : struct
			=> Properties.TryGetValue(key, out var value) && value is T typedValue
				? typedValue
				: defaultValue;

		public void SetProperty<T>(string key, T value) where T : struct
			=> Properties[key] = value;

		public void RemoveProperty(string key)
			=> Properties.Remove(key);

		public Vector3 GetPosition()
			=> transform.position;

		public Quaternion GetRotation()
			=> transform.rotation;

		public void SetPosition(Vector3 position)
			=> transform.position = position;

		public void SetRotation(Quaternion rotation)
			=> transform.rotation = rotation;

		public Vector3 GetVelocity()
			=> GetRigidbody()?.linearVelocity ?? Vector3.zero;

		public void SetVelocity(Vector3 velocity) {
			var rb = GetRigidbody();
			if (rb)
				rb.linearVelocity = velocity;
		}

		public Vector3 GetAngularVelocity()
			=> GetRigidbody()?.angularVelocity ?? Vector3.zero;

		public void SetAngularVelocity(Vector3 angular) {
			var rb = GetRigidbody();
			if (rb)
				rb.angularVelocity = angular;
		}

		public bool TryGetPhysical<T>(out T physical) where T : Physical {
			physical = this as T;
			return physical;
		}

		public bool MakePhysical()
			=> true;

		public void DestroyPhysical()
			=> DestroyImmediate(this);

		public string GetDisplay()
			=> displayName;

		public IUserIdentifier ToIdentifier()
			=> Editor.UserAPI.GetCurrent()?.ToIdentifier();

		public bool IsMaster()
			=> isMaster;

		public bool IsLocal()
			=> isLocal;

		public void SetDisplay(string display)
			=> displayName = display;

		public void Teleport(Vector3 position, Quaternion rotation) {
			transform.position = position;
			transform.rotation = rotation;
		}

		public void Teleport(Transform tr)
			=> Teleport(tr.position, tr.rotation);

		public void MovePart(ushort part, Nox.CCK.Utils.Transform tr) {
			if (part != 0) return;
			transform.position = tr.GetPosition();
			transform.rotation = tr.GetRotation();
		}

		public void Start() {
			Descriptor ??= GetComponent<IAvatarDescriptor>();
			if (Descriptor == null) {
				Debug.LogError("Avatar descriptor is not set.");
				return;
			}

			_avatar = Avatar.Setup(Descriptor);
			if (_avatar == null) {
				Debug.LogError("Failed to setup avatar.");
				return;
			}

			Logger.Log($"Avatar {_avatar} setup successfully.");
		}

		public void OnDestroy() {
			if (_avatar == null) return;
			_avatar.Dispose();
			Logger.Log($"Avatar {this} destroyed.");
		}
	}
}