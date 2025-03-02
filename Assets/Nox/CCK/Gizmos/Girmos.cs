using UnityEditor;
using UnityEngine;
using UGizmos = UnityEngine.Gizmos;

namespace Nox.CCK.Development
{
    public class Gizmos
    {
#if UNITY_EDITOR
        public static void DrawWireCapsule(Vector3 point1, Vector3 point2, float radius)
        {
            Vector3 upOffset = point2 - point1;
            Vector3 up = upOffset.Equals(default) ? Vector3.up : upOffset.normalized;
            Quaternion orientation = Quaternion.FromToRotation(Vector3.up, up);
            Vector3 forward = orientation * Vector3.forward;
            Vector3 right = orientation * Vector3.right;
            // z axis
            Handles.DrawWireArc(point2, forward, right, 180, radius);
            Handles.DrawWireArc(point1, forward, right, -180, radius);
            Handles.DrawLine(point1 + right * radius, point2 + right * radius);
            Handles.DrawLine(point1 - right * radius, point2 - right * radius);
            // x axis
            Handles.DrawWireArc(point2, right, forward, -180, radius);
            Handles.DrawWireArc(point1, right, forward, 180, radius);
            Handles.DrawLine(point1 + forward * radius, point2 + forward * radius);
            Handles.DrawLine(point1 - forward * radius, point2 - forward * radius);
            // y axis
            Handles.DrawWireDisc(point2, up, radius);
            Handles.DrawWireDisc(point1, up, radius);
        }
#endif

        public static Color color
        {
#if UNITY_EDITOR
            get => UGizmos.color;
            set => UGizmos.color = value;
#else
            get => Color.white;
            set {}
#endif
        }

        public static void DrawLine(Vector3 from, Vector3 to)
#if UNITY_EDITOR
            => UGizmos.DrawLine(from, to);
#else
            {}
#endif

        public static void DrawWireCube(Vector3 center, Vector3 size)
#if UNITY_EDITOR
            => UGizmos.DrawWireCube(center, size);
#else
            {}
#endif

        public static void DrawWireSphere(Vector3 center, float radius)
#if UNITY_EDITOR
            => UGizmos.DrawWireSphere(center, radius);
#else
            {}
#endif

        public static void DrawRay(Vector3 from, Vector3 direction)
#if UNITY_EDITOR
            => UGizmos.DrawRay(from, direction);
#else
            {}
#endif

        public static void DrawSphere(Vector3 center, float radius)
#if UNITY_EDITOR
            => UGizmos.DrawSphere(center, radius);
#else
            {}
#endif

        public static void DrawCube(Vector3 center, Vector3 size)
#if UNITY_EDITOR
            => UGizmos.DrawCube(center, size);
#else
            {}
#endif
    }
}