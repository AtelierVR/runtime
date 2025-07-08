
namespace Nox.CCK.Development
{
    public interface IGizmos
    {
        void OnDrawGizmos();

        public GizmosAttribute GetGizmosAttribute()
        {
            var type = GetType();
            var attributes = type.GetCustomAttributes(typeof(GizmosAttribute), true);
            if (attributes.Length == 0)
                return null;
            return (GizmosAttribute)attributes[0];
        }
    }
}