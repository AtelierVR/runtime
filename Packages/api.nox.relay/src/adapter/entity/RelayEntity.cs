using System.Collections.Generic;
using Nox.CCK.Utils;
using Nox.Entities;
using UnityEngine;
using Transform = Nox.CCK.Utils.Transform;

namespace api.nox.relay {
	public abstract class RelayEntity : IEntity {
		public int GetId() {
			throw new System.NotImplementedException();
		}

		public Dictionary<string, object> GetProperties() {
			throw new System.NotImplementedException();
		}

		public T GetProperty<T>(string key, T defaultValue) where T : struct {
			throw new System.NotImplementedException();
		}

		public void SetProperty<T>(string key, T value) where T : struct {
			throw new System.NotImplementedException();
		}

		public void RemoveProperty(string key) {
			throw new System.NotImplementedException();
		}

		public Vector3 GetPosition() {
			throw new System.NotImplementedException();
		}

		public Quaternion GetRotation() {
			throw new System.NotImplementedException();
		}

		public void SetPosition(Vector3 position) {
			throw new System.NotImplementedException();
		}

		public void SetRotation(Quaternion rotation) {
			throw new System.NotImplementedException();
		}

		public Vector3 GetVelocity() {
			throw new System.NotImplementedException();
		}

		public void SetVelocity(Vector3 velocity) {
			throw new System.NotImplementedException();
		}

		public Vector3 GetAngularVelocity() {
			throw new System.NotImplementedException();
		}

		public void SetAngularVelocity(Vector3 angular) {
			throw new System.NotImplementedException();
		}

		public bool TryGetPhysical<T>(out T physical) where T : Physical {
			throw new System.NotImplementedException();
		}

		public bool MakePhysical() {
			throw new System.NotImplementedException();
		}

		public void DestroyPhysical() {
			throw new System.NotImplementedException();
		}

		public void Move(Transform transform)
			=> Move(transform, TransformDeliveryType.LocalModified);

		public void Move(Transform transform, TransformDeliveryType delivery) {
			throw new System.NotImplementedException();
		}
	}
}