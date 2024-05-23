namespace Nox.CCK.Mods.Initializers
{
    public interface BaseModInitializer
    {
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