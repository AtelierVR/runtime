using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Nox.CCK.Mods
{
    public interface ShareObject
    {
        public JObject ToJson() => JObject.FromObject(this);

        /**
          * Converts this object to a new object of type T
          */
        private Dictionary<string, object> Export()
        {
            BeforeExport();
            Dictionary<string, object> dict = new();
            foreach (var prop in GetType().GetFields().Where(f => f.GetCustomAttributes(typeof(ShareObjectExportAttribute), false).Length > 0))
                dict[prop.Name] = prop.GetValue(this);
            AfterExport();
            return dict;
        }

        /**
          * Imports data from a dictionary
          * Replace all fields with the same name as a key in the dictionary with the value of that key
          * @param dict The dictionary to import from
          */
        private void Import(Dictionary<string, object> dict)
        {
            BeforeImport();
            foreach (var prop in GetType().GetFields().Where(f => f.GetCustomAttributes(typeof(ShareObjectImportAttribute), false).Length > 0))
                if (dict.ContainsKey(prop.Name))
                    if (dict[prop.Name] != null)
                    {
                        var value = dict[prop.Name];
                        var p_interfaces = prop.FieldType.GetInterfaces();
                        var v_interfaces = value.GetType().GetInterfaces();
                        if (p_interfaces.Contains(typeof(ShareObject)) && v_interfaces.Contains(typeof(ShareObject)))
                            prop.SetValue(this, (value as ShareObject).Convert(prop.FieldType));
                        else try { prop.SetValue(this, value); } catch (Exception e) { throw e; }
                    }
                    else prop.SetValue(this, null);
            AfterImport();
        }

        /**
          * Converts this object to a new object of type T
          */
        public T Convert<T>() where T : ShareObject
        {
            T obj = (T)Activator.CreateInstance(typeof(T));
            obj.Import(Export());
            return obj;
        }

        /**
          * Converts this object to a new object of type target
          */
        public ShareObject Convert(Type target)
        {
            if (!typeof(ShareObject).IsAssignableFrom(target))
                throw new ArgumentException($"Type {target} must implement ShareObject");
            var obj = (ShareObject)Activator.CreateInstance(target);
            obj.Import(Export());
            return obj;
        }

        /**
          * Action to perform before exporting data
          */
        public void BeforeImport() { }

        /**
          * Action to perform after importing data
          */
        public void AfterImport() { }

        /**
          * Action to perform before exporting data
          */
        public void BeforeExport() { }

        /**
          * Action to perform after exporting data
          */
        public void AfterExport() { }
    }

    /**
      * Attribute to mark a field for export
      */
    public class ShareObjectExportAttribute : Attribute { }

    /**
      * Attribute to mark a field for import
      */
    public class ShareObjectImportAttribute : Attribute { }
}