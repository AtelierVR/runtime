using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers
{
    public interface ModInitializer
    {
        public void OnInitialize(ModCoreAPI api) { }
        public void OnPostInitialize() { }
        public void OnUpdate() { }
        public void OnLateUpdate() { }
        public void OnFixedUpdate() { }
        public void OnDispose();

        public virtual T Call<T>(string method, params object[] args)
        {
            var type = GetType();
            var methodInfo = type.GetMethod(method);
            if (methodInfo == null) return default;
            return (T)methodInfo.Invoke(this, args);
        }

        public virtual T Property<T>(string property)
        {
            var type = GetType();
            var propertyInfo = type.GetProperty(property);
            if (propertyInfo == null) return default;
            return (T)propertyInfo.GetValue(this);
        }
    }
}