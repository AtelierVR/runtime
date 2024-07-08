using UnityEngine;

namespace Nox.Scripts
{
    public class Transform
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public Vector3 velocity = Vector3.zero;
        public Vector3 angularVelocity = Vector3.zero;

        public Transform(UnityEngine.Transform transform, Rigidbody rigidbody = null)
        {
            position = transform.position;
            rotation = transform.rotation;
            scale = transform.lossyScale;
            if (rigidbody != null)
            {
                velocity = rigidbody.linearVelocity;
                angularVelocity = rigidbody.angularVelocity;
            }
        }

        public bool IsEqual(Transform transform, float threshold = 0.01f)
        {
            return Vector3.Distance(position, transform.position) < threshold &&
                    Quaternion.Angle(rotation, transform.rotation) < threshold &&
                    Vector3.Distance(scale, transform.scale) < threshold &&
                    Vector3.Distance(velocity, transform.velocity) < threshold &&
                    Vector3.Distance(angularVelocity, transform.angularVelocity) < threshold;
        }
    }
}