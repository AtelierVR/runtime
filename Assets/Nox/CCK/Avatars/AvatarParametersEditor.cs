#if UNITY_EDITOR
using System;
using System.Linq;
using Nox.CCK.Worlds;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.CCK.Avatars
{
    [CustomEditor(typeof(AvatarParameters))]
    public class AvatarParametersEditor : Editor
    {
        public override bool UseDefaultMargins() => false;

        private VisualElement root;


        public override VisualElement CreateInspectorGUI()
        {
            root = Resources.Load<VisualTreeAsset>("api.nox.cck.avatar.avatarparameters").CloneTree();
            var parameters = target as AvatarParameters;
            if (parameters == null) return root;

            UpdateList();

            return root;
        }

        public void UpdateList()
        {
            var parameterList = root.Q<MultiColumnListView>("parameter-list");
            parameterList.Clear();
            var parameters = this.target as AvatarParameters;
            if (parameters == null) return;

            var columnType = parameterList.columns.FirstOrDefault(e => e.name == "type");
            var columnValue = parameterList.columns.FirstOrDefault(e => e.name == "value");
            var columnFlags = parameterList.columns.FirstOrDefault(e => e.name == "flags");

            var columnName = parameterList.columns.FirstOrDefault(e => e.name == "name");
            if (columnName != null)
                columnName.bindCell = (e, i) =>
                {
                    e.userData = i;
                    if (i < 0 || i >= parameters.parameters.Length) return;
                    var textField = e.Q<TextField>();
                    textField.value = parameters.parameters[i].key;
                    textField.RegisterValueChangedCallback(e =>
                    {
                        parameters.parameters[i].key = e.newValue;
                        EditorUtility.SetDirty(target);
                    });
                };

            if (columnType != null)
            {
                columnType.makeCell = () =>
                {
                    var item = new VisualElement();
                    var enumField = new EnumField();
                    enumField.Init(ParameterType.Bool);
                    enumField.RegisterValueChangedCallback(e =>
                    {
                        if (item.userData is int i and >= 0 && i < parameters.parameters.Length)
                            parameters.parameters[i].type = (ParameterType)e.newValue;
                        EditorUtility.SetDirty(target);
                    });
                    item.Add(enumField);
                    return item;
                };
                columnType.bindCell = (e, i) =>
                {
                    e.userData = i;
                    if (i >= 0 && i < parameters.parameters.Length)
                        e.Q<EnumField>().SetValueWithoutNotify(parameters.parameters[i].type);
                };
            }

            if (columnValue != null)
            {
                columnValue.makeCell = () =>
                {
                    var item = new VisualElement();
                    return item;
                };
                columnValue.bindCell = (e, i) =>
                {
                    e.userData ??= new UDataValue
                    {
                        lastType = parameters.parameters[i].type,
                        index = i
                    };

                    if (i >= 0 && i < parameters.parameters.Length)
                        switch (parameters.parameters[i].type)
                        {
                            case ParameterType.Bool:
                                var boolField = new Toggle()
                                {
                                    value = parameters.parameters[i].AsBool
                                };
                                boolField.RegisterValueChangedCallback(e =>
                                {
                                    if (e.newValue != parameters.parameters[i].AsBool)
                                        parameters.parameters[i].AsBool = e.newValue;
                                    EditorUtility.SetDirty(target);
                                });
                                e.Clear();
                                e.Add(boolField);
                                break;
                            case ParameterType.String:
                                var stringField = new TextField()
                                {
                                    value = parameters.parameters[i].AsString
                                };
                                stringField.RegisterValueChangedCallback(e =>
                                {
                                    if (e.newValue != null && e.newValue != parameters.parameters[i].AsString)
                                        parameters.parameters[i].AsString = e.newValue;
                                    EditorUtility.SetDirty(target);
                                });
                                e.Clear();
                                e.Add(stringField);
                                break;
                            case ParameterType.UInt8:
                            case ParameterType.UInt16:
                            case ParameterType.UInt32:
                                var uintField = new UnsignedIntegerField()
                                {
                                    value = parameters.parameters[i].As<byte>()
                                };
                                uintField.RegisterValueChangedCallback(e =>
                                {
                                    if (e.newValue != parameters.parameters[i].As<byte>())
                                        parameters.parameters[i].AsUInt8 = (byte)e.newValue;
                                    EditorUtility.SetDirty(target);
                                });
                                e.Clear();
                                e.Add(uintField);
                                break;
                            case ParameterType.Int8:
                            case ParameterType.Int16:
                            case ParameterType.Int32:
                                var intField = new IntegerField
                                {
                                    value = parameters.parameters[i].As<int>()
                                };
                                intField.RegisterValueChangedCallback(e =>
                                {
                                    if (e.newValue != parameters.parameters[i].As<int>())
                                        parameters.parameters[i].AsInt16 = (short)e.newValue;
                                    EditorUtility.SetDirty(target);
                                });
                                e.Clear();
                                e.Add(intField);
                                break;
                            case ParameterType.Float8:
                            case ParameterType.Float16:
                            case ParameterType.Float32:
                                var floatField = new FloatField
                                {
                                    value = parameters.parameters[i].As<float>()
                                };
                                e.Clear();
                                e.Add(floatField);
                                break;
                        }
                };
            }

            parameterList.itemsSource = parameters.parameters;
        }
    }

    public struct UDataValue
    {
        public ParameterType lastType;
        public int index;
    }
}
#endif