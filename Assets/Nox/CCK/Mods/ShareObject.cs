using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Nox.CCK.Mods
{
    public interface ShareObject
    {
        public JObject ToJson() => JObject.FromObject(this);

        private Dictionary<string, object> Export()
        {
            BeforeExport();
            Dictionary<string, object> dict = new();
            foreach (var prop in GetType().GetFields().Where(f => f.GetCustomAttributes(typeof(ShareObjectExportAttribute), false).Length > 0))
                dict[prop.Name] = prop.GetValue(this);
            AfterExport();
            return dict;
        }

        private void Import(Dictionary<string, object> dict)
        {
            BeforeImport();
            foreach (var prop in GetType().GetFields().Where(f => f.GetCustomAttributes(typeof(ShareObjectImportAttribute), false).Length > 0))
                if (dict.ContainsKey(prop.Name))
                {
                    var value = dict[prop.Name];
                    if (value == null)
                        prop.SetValue(this, value);
                    else
                    {
                        var p_interfaces = prop.FieldType.GetInterfaces();
                        var v_interfaces = value.GetType().GetInterfaces();
                        if (p_interfaces.Contains(typeof(ShareObject)) && v_interfaces.Contains(typeof(ShareObject)))
                            prop.SetValue(this, (value as ShareObject).Convert(prop.FieldType));
                        else try
                            {
                                prop.SetValue(this, value);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"Failed to set {prop.Name} to {value} ({value.GetType()}) on {GetType()}");
                                throw e;
                            }
                    }
                    AfterImport();
                }
        }

        public T Convert<T>() where T : ShareObject
        {
            T obj = (T)Activator.CreateInstance(typeof(T));
            obj.Import(Export());
            return obj;
        }


        public ShareObject Convert(Type target)
        {
            if (!typeof(ShareObject).IsAssignableFrom(target))
                throw new ArgumentException($"Type {target} must implement ShareObject");
            var obj = (ShareObject)Activator.CreateInstance(target);
            obj.Import(Export());
            return obj;
        }

        public void BeforeImport() { }
        public void AfterImport() { }
        public void BeforeExport() { }
        public void AfterExport() { }
    }

    public class ShareObjectExportAttribute : System.Attribute { }
    public class ShareObjectImportAttribute : System.Attribute { }
}