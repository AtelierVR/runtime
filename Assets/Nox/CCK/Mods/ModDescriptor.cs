using System.Collections.Generic;
using System.Linq;

namespace Nox.CCK.Mods
{
    public class Descriptor
    {
        public virtual void OnLoad() { }
        public virtual void OnUnload() { }
        public virtual void OnUpdate() { }

        private bool _enabled = false;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                if (_enabled) OnLoad();
                else OnUnload();
            }
        }

        public string Display => GetAttribute().Name;
        public string Version => GetAttribute().Version;

        public T Call<T>(string method, params object[] args)
        {
            var type = GetType();
            var methodInfo = type.GetMethod(method);
            if (methodInfo == null) return default;
            return (T)methodInfo.Invoke(this, args);
        }

        public T Property<T>(string property)
        {
            var type = GetType();
            var propertyInfo = type.GetProperty(property);
            if (propertyInfo == null) return default;
            return (T)propertyInfo.GetValue(this);
        }

        public ModAttribute GetAttribute() => (ModAttribute)GetType().GetCustomAttributes(typeof(ModAttribute), false)[0];


        public MethodWithAttribute<T>[] GetMethodWiths<T>() where T : MethodAttribute
        {
            var methods = GetType().GetMethods();
            var methodList = new List<MethodWithAttribute<T>>();
            foreach (var method in methods)
            {
                List<object> attributes = method.GetCustomAttributes(typeof(T), false).ToList();
                if (attributes.Count == 0) continue;
                methodList.Add(new MethodWithAttribute<T>
                {
                    Name = method.Name,
                    Attribute = attributes.Find(a => a.GetType() == typeof(T)) as T,
                    Method = method
                });
            }
            return methodList.ToArray();
        }


        public class MethodWithAttribute<T> where T : MethodAttribute
        {
            public string Name;
            public T Attribute;
            public System.Reflection.MethodInfo Method;
        }
    }
}