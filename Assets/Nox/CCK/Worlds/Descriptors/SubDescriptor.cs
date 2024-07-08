#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Nox.CCK.Worlds
{
    [SerializeField]
    public class SubDescriptor : BaseDescriptor
    {
#if UNITY_EDITOR
        public override void Compile()
        {
            base.Compile();
            data_Type = DescriptorType.Sub;
            EditorUtility.SetDirty(this);
        }
#endif
    }
}