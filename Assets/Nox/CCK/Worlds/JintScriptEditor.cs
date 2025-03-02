#if UNITY_EDITOR
using Jint;
using Jint.Native;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;


namespace Nox.CCK.Worlds
{
    [CustomEditor(typeof(JintScript)), CanEditMultipleObjects]
    public class JintScriptEditor : Editor
    {
        public override bool UseDefaultMargins() => false;

        public override VisualElement CreateInspectorGUI()
        {
            var root = Resources.Load<VisualTreeAsset>("api.nox.cck.world.jintscript").CloneTree();
            var script = target as JintScript;
            if (script == null) return root;

            var comp = root.Q<VisualElement>("compiled-message");
            if (script.IsCompiled)
            {
                comp.style.display = DisplayStyle.Flex;
                comp.Q<Image>("icon").image = Resources.Load<Texture2D>("warning.png");
            }
            else comp.style.display = DisplayStyle.None;

            var exports = root.Q<VisualElement>("exports");
            exports.Clear();
            foreach (var obj in script.GetExports().GetOwnProperties())
            {
                Logger.Log("Adding property: " + obj);
                if (obj.Value.Value.IsNumber())
                {
                    var field = new FloatField(obj.Key.ToString());
                    field.RegisterValueChangedCallback(evt =>
                        script.GetExports().Set(obj.Key, evt.newValue));
                    exports.Add(field);
                }
                else if (obj.Value.Value.IsString())
                {
                    var field = new TextField(obj.Key.ToString());
                    field.RegisterValueChangedCallback(evt =>
                        script.GetExports().Set(obj.Key, evt.newValue));
                    exports.Add(field);
                }
                else if (obj.Value.Value.IsBoolean())
                {
                    var field = new Toggle(obj.Key.ToString());
                    field.RegisterValueChangedCallback(evt =>
                        script.GetExports().Set(obj.Key, evt.newValue));
                    exports.Add(field);
                }
            }

            return root;
        }
    }
}
#endif // UNITY_EDITOR